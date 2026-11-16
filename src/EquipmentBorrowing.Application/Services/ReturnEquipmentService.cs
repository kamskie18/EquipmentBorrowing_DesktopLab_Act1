using System;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    public ReturnEquipmentService(IBorrowingRepository borrowingRepository, IEquipmentRepository equipmentRepository)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
    }

    public async Task<ReturnResult> ReturnAsync(
        int borrowingId,
        DateTime? actualReturnDate = null,
        CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepository.GetByIdAsync(borrowingId, cancellationToken);
        if (borrowing is null)
            return ReturnResult.Fail("Borrowing record not found.");

        // BR-13: Reject repeat return without changing state
        if (borrowing.Status == BorrowingStatus.Returned)
            return ReturnResult.Fail("This equipment borrowing has already been returned.");

        var returnDate = actualReturnDate ?? DateTime.Now;

        // BR-13: Return date cannot precede checkout date
        if (returnDate < borrowing.DateBorrowed)
            return ReturnResult.Fail("Actual return date cannot precede the checkout date.");

        var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        if (equipment is null)
            return ReturnResult.Fail("Associated equipment record not found.");

        borrowing.MarkAsReturned(returnDate);
        equipment.MarkAsAvailable();

        await _borrowingRepository.UpdateAsync(borrowing, cancellationToken);
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return ReturnResult.Success();
    }
}

public class ReturnResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private ReturnResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static ReturnResult Success() => new(true, null);
    public static ReturnResult Fail(string errorMessage) => new(false, errorMessage);
}