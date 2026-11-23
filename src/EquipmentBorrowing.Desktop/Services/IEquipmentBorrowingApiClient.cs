using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Models;

namespace EquipmentBorrowing.Desktop.Services;

public class ApiException : Exception
{
    public int? StatusCode { get; }
    public string? ErrorCode { get; }

    public ApiException(string message, int? statusCode = null, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public bool IsConnectionFailure => StatusCode == null;
    public bool IsServerError => StatusCode == 500;
}

public interface IEquipmentBorrowingApiClient
{
    Task<List<EquipmentResponse>> GetEquipmentAsync(bool? availableOnly = null);
    Task<EquipmentResponse?> GetEquipmentByIdAsync(int id);
    Task<EquipmentResponse> RegisterEquipmentAsync(string code, string name, string? description);
    Task<EquipmentResponse> UpdateEquipmentAsync(int id, string code, string name, string? description);
    Task<List<BorrowingResponse>> GetEquipmentBorrowingsAsync(int id);

    Task<List<StudentResponse>> GetStudentsAsync(string? search = null);
    Task<StudentResponse?> GetStudentByIdAsync(int id);
    Task<StudentResponse> RegisterStudentAsync(string studentNumber, string fullName, string? contact);
    Task<StudentResponse> UpdateStudentAsync(int id, string studentNumber, string fullName, string? contact);
    Task<StudentResponse> UpdateStudentEligibilityAsync(int id, bool isEligible);
    Task<List<BorrowingResponse>> GetStudentBorrowingsAsync(int id);

    Task<BorrowingResponse> BorrowEquipmentAsync(int studentId, int equipmentId, string expectedReturnDate);
    Task<CheckoutResponse> CheckoutMultipleAsync(int studentId, List<int> equipmentIds, string expectedReturnDate);
    Task<BorrowingResponse> ReturnBorrowingAsync(int id);
    Task<BorrowingResponse?> GetBorrowingByIdAsync(int id);
    Task<List<BorrowingResponse>> GetBorrowingsAsync(string? search = null, string? status = null, string? sort = null, string? direction = null);
    Task<string> ResetSeedDataAsync();
}
