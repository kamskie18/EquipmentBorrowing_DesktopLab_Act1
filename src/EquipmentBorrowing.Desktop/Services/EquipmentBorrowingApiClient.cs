using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Models;

namespace EquipmentBorrowing.Desktop.Services;

public class EquipmentBorrowingApiClient : IEquipmentBorrowingApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EquipmentBorrowingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> requestFunc)
    {
        HttpResponseMessage response;
        try
        {
            response = await requestFunc();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                "Unable to connect to the API server. Please verify the API is running at " + _httpClient.BaseAddress,
                statusCode: null,
                innerException: ex
            );
        }
        catch (Exception ex)
        {
            throw new ApiException(
                "An unexpected network or client error occurred: " + ex.Message,
                statusCode: null,
                innerException: ex
            );
        }

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return data!;
            }
            catch (Exception ex)
            {
                throw new ApiException("Failed to parse API response: " + ex.Message, (int)response.StatusCode, innerException: ex);
            }
        }

        // Handle error responses
        var statusCode = (int)response.StatusCode;
        if (statusCode == 500)
        {
            throw new ApiException("Server error (500). Please check server logs.", statusCode: 500, errorCode: "INTERNAL_ERROR");
        }

        string message = $"API error ({statusCode})";
        string? code = null;
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
            if (err != null)
            {
                message = err.Message;
                code = err.Code;
            }
        }
        catch
        {
            // fallback to status code message
        }

        throw new ApiException(message, statusCode: statusCode, errorCode: code);
    }

    // API-01
    public Task<List<EquipmentResponse>> GetEquipmentAsync(bool? availableOnly = null)
    {
        var url = "/api/v1/equipment";
        if (availableOnly.HasValue)
        {
            url += $"?available={availableOnly.Value.ToString().ToLowerInvariant()}";
        }
        return SendAsync<List<EquipmentResponse>>(() => _httpClient.GetAsync(url));
    }

    // API-07
    public Task<EquipmentResponse?> GetEquipmentByIdAsync(int id)
    {
        return SendAsync<EquipmentResponse?>(() => _httpClient.GetAsync($"/api/v1/equipment/{id}"));
    }

    // API-08
    public Task<EquipmentResponse> RegisterEquipmentAsync(string code, string name, string? description)
    {
        var req = new EquipmentCreateRequest(code, name, description);
        return SendAsync<EquipmentResponse>(() => _httpClient.PostAsJsonAsync("/api/v1/equipment", req));
    }

    // API-09
    public Task<EquipmentResponse> UpdateEquipmentAsync(int id, string code, string name, string? description)
    {
        var req = new EquipmentUpdateRequest(code, name, description);
        return SendAsync<EquipmentResponse>(() => _httpClient.PutAsJsonAsync($"/api/v1/equipment/{id}", req));
    }

    // API-16
    public Task<List<BorrowingResponse>> GetEquipmentBorrowingsAsync(int id)
    {
        return SendAsync<List<BorrowingResponse>>(() => _httpClient.GetAsync($"/api/v1/equipment/{id}/borrowings"));
    }

    // API-02
    public Task<List<StudentResponse>> GetStudentsAsync(string? search = null)
    {
        var url = "/api/v1/students";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search.Trim())}";
        }
        return SendAsync<List<StudentResponse>>(() => _httpClient.GetAsync(url));
    }

    // API-03
    public Task<StudentResponse?> GetStudentByIdAsync(int id)
    {
        return SendAsync<StudentResponse?>(() => _httpClient.GetAsync($"/api/v1/students/{id}"));
    }

    // API-04
    public Task<StudentResponse> RegisterStudentAsync(string studentNumber, string fullName, string? contact)
    {
        var req = new StudentCreateRequest(studentNumber, fullName, contact);
        return SendAsync<StudentResponse>(() => _httpClient.PostAsJsonAsync("/api/v1/students", req));
    }

    // API-05
    public Task<StudentResponse> UpdateStudentAsync(int id, string studentNumber, string fullName, string? contact)
    {
        var req = new StudentUpdateRequest(studentNumber, fullName, contact);
        return SendAsync<StudentResponse>(() => _httpClient.PutAsJsonAsync($"/api/v1/students/{id}", req));
    }

    // API-06
    public Task<StudentResponse> UpdateStudentEligibilityAsync(int id, bool isEligible)
    {
        var req = new StudentEligibilityRequest(isEligible);
        return SendAsync<StudentResponse>(() => _httpClient.PatchAsJsonAsync($"/api/v1/students/{id}/eligibility", req));
    }

    // API-15
    public Task<List<BorrowingResponse>> GetStudentBorrowingsAsync(int id)
    {
        return SendAsync<List<BorrowingResponse>>(() => _httpClient.GetAsync($"/api/v1/students/{id}/borrowings"));
    }

    // API-10
    public Task<BorrowingResponse> BorrowEquipmentAsync(int studentId, int equipmentId, string expectedReturnDate)
    {
        var req = new SingleBorrowRequest(studentId, equipmentId, expectedReturnDate);
        return SendAsync<BorrowingResponse>(() => _httpClient.PostAsJsonAsync("/api/v1/borrowings", req));
    }

    // API-11
    public Task<CheckoutResponse> CheckoutMultipleAsync(int studentId, List<int> equipmentIds, string expectedReturnDate)
    {
        var req = new CheckoutRequest(studentId, equipmentIds, expectedReturnDate);
        return SendAsync<CheckoutResponse>(() => _httpClient.PostAsJsonAsync("/api/v1/checkout-requests", req));
    }

    // API-12
    public Task<BorrowingResponse> ReturnBorrowingAsync(int id)
    {
        return SendAsync<BorrowingResponse>(() => _httpClient.PostAsJsonAsync($"/api/v1/borrowings/{id}/return", new { }));
    }

    // API-13
    public Task<BorrowingResponse?> GetBorrowingByIdAsync(int id)
    {
        return SendAsync<BorrowingResponse?>(() => _httpClient.GetAsync($"/api/v1/borrowings/{id}"));
    }

    // API-14
    public Task<List<BorrowingResponse>> GetBorrowingsAsync(string? search = null, string? status = null, string? sort = null, string? direction = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
        if (!string.IsNullOrWhiteSpace(sort)) queryParams.Add($"sort={Uri.EscapeDataString(sort.Trim())}");
        if (!string.IsNullOrWhiteSpace(direction)) queryParams.Add($"direction={Uri.EscapeDataString(direction.Trim())}");

        var url = "/api/v1/borrowings";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return SendAsync<List<BorrowingResponse>>(() => _httpClient.GetAsync(url));
    }

    public async Task<string> ResetSeedDataAsync()
    {
        var resp = await SendAsync<JsonDocument>(() => _httpClient.PostAsJsonAsync("/api/v1/dev/seed", new { }));
        return resp.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() ?? "Seeded" : "Seeded";
    }
}
