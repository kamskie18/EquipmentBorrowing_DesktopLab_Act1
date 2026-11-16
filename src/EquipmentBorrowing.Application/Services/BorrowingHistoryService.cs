using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowingHistoryService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    public BorrowingHistoryService(
        IBorrowingRepository borrowingRepository,
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository)
    {
        _borrowingRepository = borrowingRepository;
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
    }

    /// <summary>
    /// UC-07: Retrieves full borrowing history for a selected student.
    /// </summary>
    public async Task<StudentHistoryResult> GetStudentHistoryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return StudentHistoryResult.Fail("Selected student does not exist or has been removed.");

        var history = await _borrowingRepository.GetByStudentIdAsync(studentId, cancellationToken);
        return StudentHistoryResult.Success(student, history);
    }

    /// <summary>
    /// UC-08: Retrieves full borrowing history for a selected equipment item.
    /// </summary>
    public async Task<EquipmentHistoryResult> GetEquipmentHistoryAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
            return EquipmentHistoryResult.Fail("Selected equipment does not exist or has been removed.");

        var history = await _borrowingRepository.GetByEquipmentIdAsync(equipmentId, cancellationToken);
        return EquipmentHistoryResult.Success(equipment, history);
    }

    /// <summary>
    /// UC-09: Searches, filters, and sorts borrowing records across the system (BR-17).
    /// </summary>
    public async Task<IEnumerable<Borrowing>> SearchFilterSortAsync(
        string? searchText,
        BorrowingStatusFilter statusFilter,
        BorrowingSortField sortField,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        return await _borrowingRepository.SearchFilterSortAsync(
            searchText,
            statusFilter,
            sortField,
            sortDescending,
            cancellationToken);
    }
}

public class StudentHistoryResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Student? Student { get; }
    public IEnumerable<Borrowing> Borrowings { get; }

    private StudentHistoryResult(bool isSuccess, string? errorMessage, Student? student, IEnumerable<Borrowing> borrowings)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Student = student;
        Borrowings = borrowings;
    }

    public static StudentHistoryResult Success(Student student, IEnumerable<Borrowing> borrowings) =>
        new(true, null, student, borrowings);

    public static StudentHistoryResult Fail(string errorMessage) =>
        new(false, errorMessage, null, Array.Empty<Borrowing>());
}

public class EquipmentHistoryResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Equipment? Equipment { get; }
    public IEnumerable<Borrowing> Borrowings { get; }

    private EquipmentHistoryResult(bool isSuccess, string? errorMessage, Equipment? equipment, IEnumerable<Borrowing> borrowings)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Equipment = equipment;
        Borrowings = borrowings;
    }

    public static EquipmentHistoryResult Success(Equipment equipment, IEnumerable<Borrowing> borrowings) =>
        new(true, null, equipment, borrowings);

    public static EquipmentHistoryResult Fail(string errorMessage) =>
        new(false, errorMessage, null, Array.Empty<Borrowing>());
}
