using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private const int MaxActiveBorrowings = 3;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
    }

    public async Task<BorrowResult> BorrowAsync(int studentId, int equipmentId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return BorrowResult.Fail("Student does not exist.");

        if (!student.IsAllowedToBorrow)
            return BorrowResult.Fail("Student is not allowed to borrow equipment.");

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
            return BorrowResult.Fail("Equipment does not exist.");

        if (!equipment.IsAvailable)
            return BorrowResult.Fail("Equipment is currently unavailable.");

        var activeCount = await _borrowingRepository.CountActiveBorrowingsAsync(studentId, cancellationToken);
        if (activeCount >= MaxActiveBorrowings)
            return BorrowResult.Fail("Student has reached the maximum number of active borrowings.");

        var borrowing = new Borrowing(
            id: new Random().Next(1000, 9999),
            studentId: studentId,
            equipmentId: equipmentId,
            dateBorrowed: DateTime.Now,
            expectedReturnDate: DateTime.Now.AddDays(7));

        equipment.MarkAsBorrowed();
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
        await _borrowingRepository.AddAsync(borrowing, cancellationToken);

        return BorrowResult.Success(borrowing);
    }
}

public class BorrowResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Borrowing? Borrowing { get; }

    private BorrowResult(bool isSuccess, string? errorMessage, Borrowing? borrowing)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Borrowing = borrowing;
    }

    public static BorrowResult Success(Borrowing borrowing) => new(true, null, borrowing);
    public static BorrowResult Fail(string errorMessage) => new(false, errorMessage, null);
}