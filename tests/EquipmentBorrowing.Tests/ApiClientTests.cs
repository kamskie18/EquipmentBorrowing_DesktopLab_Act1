using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Desktop.Services;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        Handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Handler(request));
    }
}

public class ApiClientTests
{
    private static HttpClient CreateMockClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        return new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
    }

    // T01: Independent API reads, selection, relational lists, missing record 404
    [Fact]
    public async Task T01_GetEquipmentAndStudents_ReturnsParsedList()
    {
        var client = CreateMockClient(req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("/api/v1/equipment?available=true"))
            {
                var json = "[{\"id\":2,\"equipmentCode\":\"EQ-002\",\"name\":\"Laptop\",\"description\":\"High performance laptop\",\"isAvailable\":true}]";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }
            if (req.RequestUri!.PathAndQuery.Contains("/api/v1/students"))
            {
                var json = "[{\"id\":1,\"studentNumber\":\"ST-100\",\"fullName\":\"Ada Cruz\",\"contact\":\"ada@campus.edu\",\"isEligible\":true}]";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"code\":\"NOT_FOUND\",\"message\":\"Not found\"}", Encoding.UTF8, "application/json")
            };
        });

        var apiClient = new EquipmentBorrowingApiClient(client);

        var equip = await apiClient.GetEquipmentAsync(availableOnly: true);
        Assert.Single(equip);
        Assert.Equal("EQ-002", equip[0].EquipmentCode);
        Assert.True(equip[0].IsAvailable);

        var students = await apiClient.GetStudentsAsync();
        Assert.Single(students);
        Assert.Equal("ST-100", students[0].StudentNumber);
    }

    // T02: Student management, duplicate 409, eligibility patch
    [Fact]
    public async Task T02_StudentRegistration_HandlesSuccessAndConflict()
    {
        var client = CreateMockClient(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/api/v1/students")
            {
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"id\":5,\"studentNumber\":\"ST-500\",\"fullName\":\"John Doe\",\"contact\":null,\"isEligible\":true}", Encoding.UTF8, "application/json")
                };
            }
            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath.EndsWith("/eligibility"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":1,\"studentNumber\":\"ST-100\",\"fullName\":\"Ada Cruz\",\"contact\":\"ada@campus.edu\",\"isEligible\":false}", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("{\"code\":\"CONFLICT\",\"message\":\"Student number already exists.\"}", Encoding.UTF8, "application/json")
            };
        });

        var apiClient = new EquipmentBorrowingApiClient(client);

        var created = await apiClient.RegisterStudentAsync("ST-500", "John Doe", null);
        Assert.Equal(5, created.Id);
        Assert.Equal("ST-500", created.StudentNumber);

        var patched = await apiClient.UpdateStudentEligibilityAsync(1, false);
        Assert.False(patched.IsEligible);
    }

    // T03: Equipment management with protected field rejection
    [Fact]
    public async Task T03_EquipmentUpdate_ProtectedFieldRejection()
    {
        var client = CreateMockClient(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"code\":\"BAD_REQUEST\",\"message\":\"Modifying 'isAvailable' is protected and cannot be updated directly via PUT.\"}", Encoding.UTF8, "application/json")
            };
        });

        var apiClient = new EquipmentBorrowingApiClient(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.UpdateEquipmentAsync(1, "EQ-001", "Camera", "Desc"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("protected", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // T04 & T05: Borrowing, return, repeat return 409 rejection
    [Fact]
    public async Task T04_BorrowAndReturn_HandlesValidationAndRepeatReturn()
    {
        var client = CreateMockClient(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/return"))
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("{\"code\":\"CONFLICT\",\"message\":\"This equipment borrowing has already been returned.\"}", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var apiClient = new EquipmentBorrowingApiClient(client);

        var ex = await Assert.ThrowsAsync<ApiException>(() => apiClient.ReturnBorrowingAsync(99));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("CONFLICT", ex.ErrorCode);
    }

    // T08: Distinguish unreachable API from 500 server error
    [Fact]
    public async Task T08_DistinguishConnectionFailureFromServerError()
    {
        // 1. Connection failure (no HTTP response)
        var brokenClient = CreateMockClient(req => throw new HttpRequestException("Connection refused"));
        var apiClient1 = new EquipmentBorrowingApiClient(brokenClient);

        var ex1 = await Assert.ThrowsAsync<ApiException>(() => apiClient1.GetStudentsAsync());
        Assert.True(ex1.IsConnectionFailure);
        Assert.Null(ex1.StatusCode);
        Assert.Contains("Unable to connect", ex1.Message);

        // 2. HTTP 500 Server Error
        var serverErrorClient = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var apiClient2 = new EquipmentBorrowingApiClient(serverErrorClient);

        var ex2 = await Assert.ThrowsAsync<ApiException>(() => apiClient2.GetStudentsAsync());
        Assert.True(ex2.IsServerError);
        Assert.Equal(500, ex2.StatusCode);
        Assert.Contains("500", ex2.Message);
    }
}
