using System;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class StudentManagementService
{
    private readonly IStudentRepository _studentRepository;

    public StudentManagementService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<StudentResult> RegisterStudentAsync(
        string studentNumber,
        string name,
        string? contactInfo = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            return StudentResult.Fail("Student number is required.");

        if (string.IsNullOrWhiteSpace(name))
            return StudentResult.Fail("Full name is required.");

        var trimmedNumber = studentNumber.Trim();
        var trimmedName = name.Trim();
        var trimmedContact = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();

        if (await _studentRepository.ExistsWithStudentNumberAsync(trimmedNumber, cancellationToken: cancellationToken))
            return StudentResult.Fail($"A student with number '{trimmedNumber}' already exists.");

        var student = new Student(
            id: 0,
            studentNumber: trimmedNumber,
            name: trimmedName,
            contactInfo: trimmedContact,
            isAllowedToBorrow: true);

        await _studentRepository.AddAsync(student, cancellationToken);
        return StudentResult.Success(student, "Student registered successfully.");
    }

    public async Task<StudentResult> UpdateStudentAsync(
        int id,
        string studentNumber,
        string name,
        string? contactInfo = null,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(id, cancellationToken);
        if (student is null)
            return StudentResult.Fail("Student not found.");

        if (string.IsNullOrWhiteSpace(studentNumber))
            return StudentResult.Fail("Student number is required.");

        if (string.IsNullOrWhiteSpace(name))
            return StudentResult.Fail("Full name is required.");

        var trimmedNumber = studentNumber.Trim();
        var trimmedName = name.Trim();
        var trimmedContact = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();

        if (await _studentRepository.ExistsWithStudentNumberAsync(trimmedNumber, excludeId: id, cancellationToken: cancellationToken))
            return StudentResult.Fail($"A student with number '{trimmedNumber}' already exists.");

        student.UpdateDetails(trimmedNumber, trimmedName, trimmedContact);
        await _studentRepository.UpdateAsync(student, cancellationToken);

        return StudentResult.Success(student, "Student information updated successfully.");
    }

    public async Task<StudentResult> ChangeEligibilityAsync(
        int id,
        bool permit,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(id, cancellationToken);
        if (student is null)
            return StudentResult.Fail("Student not found.");

        if (student.IsAllowedToBorrow == permit)
        {
            var state = permit ? "permitted to borrow" : "restricted from borrowing";
            return StudentResult.Success(student, $"Student '{student.Name}' is already {state}. No changes made.");
        }

        student.SetEligibility(permit);
        await _studentRepository.UpdateAsync(student, cancellationToken);

        var message = permit
            ? $"Student '{student.Name}' is now permitted to borrow."
            : $"Student '{student.Name}' has been restricted from borrowing. Active loans may still be returned.";

        return StudentResult.Success(student, message);
    }
}

public class StudentResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? FeedbackMessage { get; }
    public Student? Student { get; }

    private StudentResult(bool isSuccess, string? errorMessage, string? feedbackMessage, Student? student)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        FeedbackMessage = feedbackMessage;
        Student = student;
    }

    public static StudentResult Success(Student student, string feedbackMessage) =>
        new(true, null, feedbackMessage, student);

    public static StudentResult Fail(string errorMessage) =>
        new(false, errorMessage, null, null);
}
