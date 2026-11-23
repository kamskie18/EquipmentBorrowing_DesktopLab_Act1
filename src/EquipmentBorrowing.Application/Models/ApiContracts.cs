using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EquipmentBorrowing.Application.Models;

public record ErrorResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message
);

public record StudentResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("studentNumber")] string StudentNumber,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("contact")] string? Contact,
    [property: JsonPropertyName("isEligible")] bool IsEligible
);

public record StudentCreateRequest(
    [property: JsonPropertyName("studentNumber")] string? StudentNumber,
    [property: JsonPropertyName("fullName")] string? FullName,
    [property: JsonPropertyName("contact")] string? Contact
);

public record StudentUpdateRequest(
    [property: JsonPropertyName("studentNumber")] string? StudentNumber,
    [property: JsonPropertyName("fullName")] string? FullName,
    [property: JsonPropertyName("contact")] string? Contact
);

public record StudentEligibilityRequest(
    [property: JsonPropertyName("isEligible")] bool? IsEligible
);

public record EquipmentResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("equipmentCode")] string EquipmentCode,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isAvailable")] bool IsAvailable
);

public record EquipmentCreateRequest(
    [property: JsonPropertyName("equipmentCode")] string? EquipmentCode,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description
);

public record EquipmentUpdateRequest(
    [property: JsonPropertyName("equipmentCode")] string? EquipmentCode,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isAvailable")] bool? IsAvailable = null
);

public record BorrowingResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("studentId")] int StudentId,
    [property: JsonPropertyName("equipmentId")] int EquipmentId,
    [property: JsonPropertyName("borrowedDate")] string BorrowedDate,
    [property: JsonPropertyName("expectedReturnDate")] string ExpectedReturnDate,
    [property: JsonPropertyName("returnedDate")] string? ReturnedDate,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("studentNumber")] string? StudentNumber = null,
    [property: JsonPropertyName("studentName")] string? StudentName = null,
    [property: JsonPropertyName("equipmentCode")] string? EquipmentCode = null,
    [property: JsonPropertyName("equipmentName")] string? EquipmentName = null
);

public record SingleBorrowRequest(
    [property: JsonPropertyName("studentId")] int StudentId,
    [property: JsonPropertyName("equipmentId")] int EquipmentId,
    [property: JsonPropertyName("expectedReturnDate")] string ExpectedReturnDate
);

public record CheckoutRequest(
    [property: JsonPropertyName("studentId")] int StudentId,
    [property: JsonPropertyName("equipmentIds")] List<int>? EquipmentIds,
    [property: JsonPropertyName("expectedReturnDate")] string ExpectedReturnDate
);

public record CheckoutResponse(
    [property: JsonPropertyName("borrowings")] List<BorrowingResponse> Borrowings
);
