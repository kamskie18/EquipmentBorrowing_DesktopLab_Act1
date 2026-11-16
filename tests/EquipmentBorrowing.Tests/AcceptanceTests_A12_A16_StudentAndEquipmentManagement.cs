using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class AcceptanceTests_A12_A16_StudentAndEquipmentManagement
{
    [Fact]
    public async Task A12_UC12_RegisterStudent_SuccessAndValidationRejections()
    {
        var studentRepo = new InMemoryStudentRepository();
        var service = new StudentManagementService(studentRepo);

        // 1. Success: Valid student with default eligibility
        var result = await service.RegisterStudentAsync("S1099", "Carlos Yulo", "carlos@gym.ph");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Student);
        Assert.Equal("S1099", result.Student!.StudentNumber);
        Assert.Equal("Carlos Yulo", result.Student.Name);
        Assert.Equal("carlos@gym.ph", result.Student.ContactInfo);
        Assert.True(result.Student.IsAllowedToBorrow);

        // 2. Reject blank student number
        var blankNumResult = await service.RegisterStudentAsync("   ", "Carlos Yulo", null);
        Assert.False(blankNumResult.IsSuccess);
        Assert.Contains("student number", blankNumResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 3. Reject blank name
        var blankNameResult = await service.RegisterStudentAsync("S1100", "   ", null);
        Assert.False(blankNameResult.IsSuccess);
        Assert.Contains("name", blankNameResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 4. Reject duplicate student number (case-insensitive & trimmed variation)
        var dupResult = await service.RegisterStudentAsync("  s1099  ", "Another Person", null);
        Assert.False(dupResult.IsSuccess);
        Assert.Contains("already exists", dupResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A13_UC13_UpdateStudent_SuccessAndValidationRejections()
    {
        var studentRepo = new InMemoryStudentRepository();
        var service = new StudentManagementService(studentRepo);

        // Update Student 1 (Juan Dela Cruz)
        var updateResult = await service.UpdateStudentAsync(1, "S1001-REV", "Juan D. Cruz", null);
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("S1001-REV", updateResult.Student!.StudentNumber);
        Assert.Equal("Juan D. Cruz", updateResult.Student.Name);
        Assert.Null(updateResult.Student.ContactInfo); // Blank cleared it (BR-03)

        // Reject duplicate student number of another student (Student 2 has S1002)
        var dupResult = await service.UpdateStudentAsync(1, "S1002", "Juan D. Cruz", null);
        Assert.False(dupResult.IsSuccess);
        Assert.Contains("already exists", dupResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Reject blank name
        var blankNameResult = await service.UpdateStudentAsync(1, "S1001", "   ", null);
        Assert.False(blankNameResult.IsSuccess);
    }

    [Fact]
    public async Task A14_UC14_ChangeEligibility_AllowsReturnWhileRestricted_AndProvidesFeedback()
    {
        var studentRepo = new InMemoryStudentRepository();
        var service = new StudentManagementService(studentRepo);

        // 1. Restrict Student 1 (who was previously permitted)
        var restrictResult = await service.ChangeEligibilityAsync(1, permit: false);
        Assert.True(restrictResult.IsSuccess);
        Assert.False(restrictResult.Student!.IsAllowedToBorrow);
        Assert.Contains("restricted", restrictResult.FeedbackMessage, StringComparison.OrdinalIgnoreCase);

        // 2. Repeating current choice gives understandable feedback without error (BR-04)
        var repeatResult = await service.ChangeEligibilityAsync(1, permit: false);
        Assert.True(repeatResult.IsSuccess);
        Assert.Contains("already restricted", repeatResult.FeedbackMessage, StringComparison.OrdinalIgnoreCase);

        // 3. Permit Student 1 again
        var permitResult = await service.ChangeEligibilityAsync(1, permit: true);
        Assert.True(permitResult.IsSuccess);
        Assert.True(permitResult.Student!.IsAllowedToBorrow);
        Assert.Contains("permitted", permitResult.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A15_UC15_RegisterEquipment_SuccessAndValidationRejections()
    {
        var equipRepo = new InMemoryEquipmentRepository();
        var service = new EquipmentManagementService(equipRepo);

        // 1. Success
        var result = await service.RegisterEquipmentAsync("EQ-999", "3D Printer", "Creality Ender 3");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Equipment);
        Assert.Equal("EQ-999", result.Equipment!.EquipmentIdentifier);
        Assert.True(result.Equipment.IsAvailable);

        // 2. Reject blank identifier
        var blankIdResult = await service.RegisterEquipmentAsync("  ", "Tool", null);
        Assert.False(blankIdResult.IsSuccess);

        // 3. Reject blank name
        var blankNameResult = await service.RegisterEquipmentAsync("EQ-1000", "  ", null);
        Assert.False(blankNameResult.IsSuccess);

        // 4. Reject duplicate identifier (case-insensitive)
        var dupResult = await service.RegisterEquipmentAsync("  eq-999  ", "Another Printer", null);
        Assert.False(dupResult.IsSuccess);
        Assert.Contains("already exists", dupResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A16_UC16_UpdateEquipment_SuccessAndValidationRejections()
    {
        var equipRepo = new InMemoryEquipmentRepository();
        var service = new EquipmentManagementService(equipRepo);

        // Update Equipment 1
        var updateResult = await service.UpdateEquipmentAsync(1, "EQ-001-REV", "Digital Multimeter Pro", null);
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("EQ-001-REV", updateResult.Equipment!.EquipmentIdentifier);
        Assert.Equal("Digital Multimeter Pro", updateResult.Equipment.Name);
        Assert.Null(updateResult.Equipment.Description); // Blank cleared it (BR-06/07)

        // Reject duplicate identifier of another item (EQ-002)
        var dupResult = await service.UpdateEquipmentAsync(1, "EQ-002", "Multimeter", null);
        Assert.False(dupResult.IsSuccess);
        Assert.Contains("already exists", dupResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
