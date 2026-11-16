using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class MultiItemBorrowService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IUnitOfWork _unitOfWork;
    public const int MaxActiveBorrowings = 3;

    public MultiItemBorrowService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository,
        IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executes the atomic multi-item checkout use case (UC-06).
    /// Commits all selected borrowings and availability changes as one atomic transaction, or none.
    /// </summary>
    /// <param name="studentId">The borrowing student's ID.</param>
    /// <param name="equipmentIds">List of equipment IDs requested.</param>
    /// <param name="expectedReturnDate">The agreed expected return date.</param>
    /// <param name="simulateFailureAfterFirstWrite">Controlled test seam for Acceptance A06 / atomic rollback demonstration.</param>
    public async Task<MultiItemBorrowResult> CheckOutMultipleAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateTime expectedReturnDate,
        bool simulateFailureAfterFirstWrite = false,
        CancellationToken cancellationToken = default)
    {
        var checkoutDate = DateTime.Now;

        // BR-11: Expected return date must be later than checkout date
        if (expectedReturnDate <= checkoutDate)
            return MultiItemBorrowResult.Fail("Expected return date must be later than the checkout date.");

        // BR-12: Must have at least two items
        if (equipmentIds == null || equipmentIds.Count < 2)
            return MultiItemBorrowResult.Fail("Multi-item checkout requires at least two distinct items.");

        // BR-12: Reject duplicate items in the request
        var distinctCount = equipmentIds.Distinct().Count();
        if (distinctCount != equipmentIds.Count)
            return MultiItemBorrowResult.Fail("Duplicate items detected in request. All selected items must be distinct.");

        // Begin Atomic Transaction (BR-15)
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Revalidation at transaction boundary (BR-15)
            // 1. Verify Student and Eligibility (BR-08)
            var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
            if (student is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MultiItemBorrowResult.Fail("Student does not exist.");
            }

            if (!student.IsAllowedToBorrow)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MultiItemBorrowResult.Fail($"Student '{student.Name}' is currently restricted from borrowing.");
            }

            // 2. Verify Active Limit (BR-09)
            var currentActive = await _borrowingRepository.CountActiveBorrowingsAsync(studentId, cancellationToken);
            if (currentActive + equipmentIds.Count > MaxActiveBorrowings)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MultiItemBorrowResult.Fail(
                    $"Limit exceeded: Student has {currentActive} active item(s). Requesting {equipmentIds.Count} items would exceed the maximum limit of {MaxActiveBorrowings}.");
            }

            // 3. Verify each equipment exists and is currently available (BR-10)
            var loadedEquipment = new List<Equipment>();
            foreach (var equipId in equipmentIds)
            {
                var equip = await _equipmentRepository.GetByIdAsync(equipId, cancellationToken);
                if (equip is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return MultiItemBorrowResult.Fail($"Equipment with ID {equipId} does not exist.");
                }

                if (!equip.IsAvailable)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return MultiItemBorrowResult.Fail($"Equipment '{equip.Name}' ({equip.EquipmentIdentifier}) is currently unavailable.");
                }

                loadedEquipment.Add(equip);
            }

            // 4. Perform atomic writes
            var createdBorrowings = new List<Borrowing>();
            for (int i = 0; i < loadedEquipment.Count; i++)
            {
                var equip = loadedEquipment[i];

                var borrowing = new Borrowing(
                    id: 0,
                    studentId: studentId,
                    equipmentId: equip.Id,
                    dateBorrowed: checkoutDate,
                    expectedReturnDate: expectedReturnDate);

                equip.MarkAsBorrowed();

                await _equipmentRepository.UpdateAsync(equip, cancellationToken);
                await _borrowingRepository.AddAsync(borrowing, cancellationToken);

                createdBorrowings.Add(borrowing);

                // Controlled test seam for Acceptance A06 (force failure after at least one write is attempted)
                if (simulateFailureAfterFirstWrite && i == 0)
                {
                    throw new InvalidOperationException("Controlled in-transaction failure simulated after first item write for atomicity verification.");
                }
            }

            // Commit transaction atomically
            await transaction.CommitAsync(cancellationToken);

            return MultiItemBorrowResult.Success(
                createdBorrowings,
                $"Successfully checked out {createdBorrowings.Count} items to {student.Name}. Due: {expectedReturnDate:MMM dd, yyyy}.");
        }
        catch (Exception ex)
        {
            // Atomic rollback on any failure
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch
            {
                // Preserve original exception context under BR-18
            }

            return MultiItemBorrowResult.Fail($"Checkout failed and rolled back cleanly: {ex.Message}");
        }
    }
}

public class MultiItemBorrowResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? FeedbackMessage { get; }
    public IReadOnlyList<Borrowing> Borrowings { get; }

    private MultiItemBorrowResult(
        bool isSuccess,
        string? errorMessage,
        string? feedbackMessage,
        IReadOnlyList<Borrowing> borrowings)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        FeedbackMessage = feedbackMessage;
        Borrowings = borrowings;
    }

    public static MultiItemBorrowResult Success(IReadOnlyList<Borrowing> borrowings, string feedbackMessage) =>
        new(true, null, feedbackMessage, borrowings);

    public static MultiItemBorrowResult Fail(string errorMessage) =>
        new(false, errorMessage, null, Array.Empty<Borrowing>());
}
