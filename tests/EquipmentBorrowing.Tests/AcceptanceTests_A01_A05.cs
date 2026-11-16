using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class AcceptanceTests_A01_A05
{
    private (InMemoryStudentRepository students, InMemoryEquipmentRepository equipment, InMemoryBorrowingRepository borrowings) CreateRepositories()
    {
        return (new InMemoryStudentRepository(), new InMemoryEquipmentRepository(), new InMemoryBorrowingRepository());
    }

    [Fact]
    public async Task A01_UC01_ViewEquipmentCatalog_DisplaysAllAndAvailability()
    {
        var (_, equipRepo, _) = CreateRepositories();
        var catalog = (await equipRepo.GetAllAsync()).ToList();

        Assert.NotEmpty(catalog);
        Assert.Contains(catalog, e => e.IsAvailable);
        Assert.Contains(catalog, e => !e.IsAvailable);
        Assert.All(catalog, e => Assert.False(string.IsNullOrWhiteSpace(e.EquipmentIdentifier)));
    }

    [Fact]
    public async Task A02_UC02_FindAvailableEquipment_ExcludesUnavailableItems()
    {
        var (_, equipRepo, _) = CreateRepositories();
        var available = (await equipRepo.GetAvailableAsync()).ToList();

        Assert.NotEmpty(available);
        Assert.All(available, e => Assert.True(e.IsAvailable));
    }

    [Fact]
    public async Task A03_UC03_BorrowEquipment_SuccessAndValidationRejections()
    {
        var (studentRepo, equipRepo, borrowingRepo) = CreateRepositories();
        var service = new BorrowEquipmentService(studentRepo, equipRepo, borrowingRepo);

        // 1. Success: Issue available item to eligible student
        var successResult = await service.BorrowAsync(1, 1, DateTime.Now.AddDays(7));
        Assert.True(successResult.IsSuccess);
        Assert.NotNull(successResult.Borrowing);
        Assert.Equal(BorrowingStatus.Active, successResult.Borrowing!.Status);

        var borrowedItem = await equipRepo.GetByIdAsync(1);
        Assert.False(borrowedItem!.IsAvailable);

        // 2. Reject unavailable equipment
        var unavailableResult = await service.BorrowAsync(1, 1, DateTime.Now.AddDays(7));
        Assert.False(unavailableResult.IsSuccess);
        Assert.Contains("unavailable", unavailableResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 3. Reject restricted/ineligible student (Student 2 is restricted)
        var ineligibleResult = await service.BorrowAsync(2, 3, DateTime.Now.AddDays(7));
        Assert.False(ineligibleResult.IsSuccess);
        Assert.Contains("restricted", ineligibleResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 4. Reject invalid return date (return <= checkout)
        var invalidDateResult = await service.BorrowAsync(3, 3, DateTime.Now.AddDays(-1));
        Assert.False(invalidDateResult.IsSuccess);
        Assert.Contains("date", invalidDateResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 5. Reject missing student
        var missingStudentResult = await service.BorrowAsync(999, 3, DateTime.Now.AddDays(7));
        Assert.False(missingStudentResult.IsSuccess);
        Assert.Contains("not exist", missingStudentResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 6. Reject over-limit (limit = 3 active items)
        // Student 1 already has 1 active borrowing from step 1. Let's add 2 more.
        var equip4 = new Equipment(4, "EQ-004", "Power Supply", null, true);
        var equip5 = new Equipment(5, "EQ-005", "Logic Analyzer", null, true);
        var equip6 = new Equipment(6, "EQ-006", "Breadboard", null, true);
        await equipRepo.AddAsync(equip4);
        await equipRepo.AddAsync(equip5);
        await equipRepo.AddAsync(equip6);

        await service.BorrowAsync(1, 4, DateTime.Now.AddDays(7));
        await service.BorrowAsync(1, 5, DateTime.Now.AddDays(7));

        // Now student 1 has 3 active loans. The 4th must be rejected under BR-09
        var overLimitResult = await service.BorrowAsync(1, 6, DateTime.Now.AddDays(7));
        Assert.False(overLimitResult.IsSuccess);
        Assert.Contains("maximum", overLimitResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A04_UC04_ViewActiveBorrowings_ExcludesReturnedRecords()
    {
        var (_, _, borrowingRepo) = CreateRepositories();

        var b1 = new Borrowing(1, 1, 1, DateTime.Now, DateTime.Now.AddDays(7));
        var b2 = new Borrowing(2, 1, 2, DateTime.Now, DateTime.Now.AddDays(7));
        b2.MarkAsReturned(DateTime.Now);

        await borrowingRepo.AddAsync(b1);
        await borrowingRepo.AddAsync(b2);

        var active = (await borrowingRepo.GetActiveAsync()).ToList();

        Assert.Single(active);
        Assert.Equal(1, active[0].Id);
        Assert.Equal(BorrowingStatus.Active, active[0].Status);
    }

    [Fact]
    public async Task A05_UC05_ReturnEquipment_SuccessAndRepeatRejection()
    {
        var (_, equipRepo, borrowingRepo) = CreateRepositories();
        var returnService = new ReturnEquipmentService(borrowingRepo, equipRepo);

        // Set up active borrowing
        var item = await equipRepo.GetByIdAsync(1);
        item!.MarkAsBorrowed();
        await equipRepo.UpdateAsync(item);

        var borrowing = new Borrowing(101, 1, 1, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(5));
        await borrowingRepo.AddAsync(borrowing);

        // 1. Success return
        var returnResult = await returnService.ReturnAsync(101, DateTime.Now);
        Assert.True(returnResult.IsSuccess);

        var updatedBorrowing = await borrowingRepo.GetByIdAsync(101);
        Assert.Equal(BorrowingStatus.Returned, updatedBorrowing!.Status);
        Assert.NotNull(updatedBorrowing.ActualReturnDate);

        var updatedEquip = await equipRepo.GetByIdAsync(1);
        Assert.True(updatedEquip!.IsAvailable);

        // 2. Repeat return attempt must be rejected under BR-13
        var repeatResult = await returnService.ReturnAsync(101, DateTime.Now);
        Assert.False(repeatResult.IsSuccess);
        Assert.Contains("already been returned", repeatResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 3. Stale / missing selection
        var missingResult = await returnService.ReturnAsync(9999, DateTime.Now);
        Assert.False(missingResult.IsSuccess);
        Assert.Contains("not found", missingResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
