using System;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class EquipmentManagementService
{
    private readonly IEquipmentRepository _equipmentRepository;

    public EquipmentManagementService(IEquipmentRepository equipmentRepository)
    {
        _equipmentRepository = equipmentRepository;
    }

    public async Task<EquipmentResult> RegisterEquipmentAsync(
        string identifier,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return EquipmentResult.Fail("Equipment identifier is required.");

        if (string.IsNullOrWhiteSpace(name))
            return EquipmentResult.Fail("Equipment name is required.");

        var trimmedIdentifier = identifier.Trim();
        var trimmedName = name.Trim();
        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (await _equipmentRepository.ExistsWithIdentifierAsync(trimmedIdentifier, cancellationToken: cancellationToken))
            return EquipmentResult.Fail($"Equipment with identifier '{trimmedIdentifier}' already exists.");

        var equipment = new Equipment(
            id: 0,
            equipmentIdentifier: trimmedIdentifier,
            name: trimmedName,
            description: trimmedDescription,
            isAvailable: true);

        await _equipmentRepository.AddAsync(equipment, cancellationToken);
        return EquipmentResult.Success(equipment, "Equipment registered successfully as available.");
    }

    public async Task<EquipmentResult> UpdateEquipmentAsync(
        int id,
        string identifier,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(id, cancellationToken);
        if (equipment is null)
            return EquipmentResult.Fail("Equipment not found.");

        if (string.IsNullOrWhiteSpace(identifier))
            return EquipmentResult.Fail("Equipment identifier is required.");

        if (string.IsNullOrWhiteSpace(name))
            return EquipmentResult.Fail("Equipment name is required.");

        var trimmedIdentifier = identifier.Trim();
        var trimmedName = name.Trim();
        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (await _equipmentRepository.ExistsWithIdentifierAsync(trimmedIdentifier, excludeId: id, cancellationToken: cancellationToken))
            return EquipmentResult.Fail($"Equipment with identifier '{trimmedIdentifier}' already exists.");

        equipment.UpdateDetails(trimmedIdentifier, trimmedName, trimmedDescription);
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return EquipmentResult.Success(equipment, "Equipment information updated successfully.");
    }
}

public class EquipmentResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? FeedbackMessage { get; }
    public Equipment? Equipment { get; }

    private EquipmentResult(bool isSuccess, string? errorMessage, string? feedbackMessage, Equipment? equipment)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        FeedbackMessage = feedbackMessage;
        Equipment = equipment;
    }

    public static EquipmentResult Success(Equipment equipment, string feedbackMessage) =>
        new(true, null, feedbackMessage, equipment);

    public static EquipmentResult Fail(string errorMessage) =>
        new(false, errorMessage, null, null);
}
