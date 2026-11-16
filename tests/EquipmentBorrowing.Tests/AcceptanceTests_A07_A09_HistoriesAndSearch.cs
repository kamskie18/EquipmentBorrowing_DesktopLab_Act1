using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class AcceptanceTests_A07_A09_HistoriesAndSearch
{
    private (InMemoryStudentRepository students, InMemoryEquipmentRepository equipment, InMemoryBorrowingRepository borrowings, BorrowingHistoryService historyService) CreateSetup()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var historyService = new BorrowingHistoryService(borrowingRepo, studentRepo, equipRepo);
        return (studentRepo, equipRepo, borrowingRepo, historyService);
    }

    [Fact]
    public async Task A07_UC07_StudentBorrowingHistory_DistinguishesStudentsAndHandlesEmpty()
    {
        var (_, _, borrowingRepo, historyService) = CreateSetup();

        // Student 1 has 1 active and 1 returned borrowing
        var b1 = new Borrowing(1, 1, 1, DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-3));
        b1.MarkAsReturned(DateTime.Now.AddDays(-4));
        var b2 = new Borrowing(2, 1, 3, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(6));

        // Student 2 has 1 active borrowing
        var b3 = new Borrowing(3, 2, 2, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(5));

        await borrowingRepo.AddAsync(b1);
        await borrowingRepo.AddAsync(b2);
        await borrowingRepo.AddAsync(b3);

        // Fetch Student 1 history
        var result1 = await historyService.GetStudentHistoryAsync(1);
        Assert.True(result1.IsSuccess);
        var s1List = result1.Borrowings.ToList();
        Assert.Equal(2, s1List.Count);
        Assert.All(s1List, b => Assert.Equal(1, b.StudentId));

        // Fetch Student 2 history
        var result2 = await historyService.GetStudentHistoryAsync(2);
        Assert.True(result2.IsSuccess);
        var s2List = result2.Borrowings.ToList();
        Assert.Single(s2List);
        Assert.Equal(2, s2List[0].StudentId);

        // Student 3 has no history
        var result3 = await historyService.GetStudentHistoryAsync(3);
        Assert.True(result3.IsSuccess);
        Assert.Empty(result3.Borrowings);
    }

    [Fact]
    public async Task A08_UC08_EquipmentBorrowingHistory_DistinguishesItemsAndHandlesEmpty()
    {
        var (_, _, borrowingRepo, historyService) = CreateSetup();

        // Equipment 1 was borrowed by Student 1 (returned) and then by Student 3 (active)
        var b1 = new Borrowing(1, 1, 1, DateTime.Now.AddDays(-20), DateTime.Now.AddDays(-10));
        b1.MarkAsReturned(DateTime.Now.AddDays(-12));
        var b2 = new Borrowing(2, 3, 1, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(5));

        // Equipment 3 was borrowed by Student 1 (active)
        var b3 = new Borrowing(3, 1, 3, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(6));

        await borrowingRepo.AddAsync(b1);
        await borrowingRepo.AddAsync(b2);
        await borrowingRepo.AddAsync(b3);

        // Fetch Equipment 1 history
        var result1 = await historyService.GetEquipmentHistoryAsync(1);
        Assert.True(result1.IsSuccess);
        var eq1List = result1.Borrowings.ToList();
        Assert.Equal(2, eq1List.Count);
        Assert.All(eq1List, b => Assert.Equal(1, b.EquipmentId));

        // Fetch Equipment 2 history (empty)
        var result2 = await historyService.GetEquipmentHistoryAsync(2);
        Assert.True(result2.IsSuccess);
        Assert.Empty(result2.Borrowings);
    }

    [Fact]
    public async Task A09_UC09_SearchFilterSort_ExecutesCriteriaAndReset()
    {
        var (_, _, borrowingRepo, historyService) = CreateSetup();

        // Populate borrowings
        var b1 = new Borrowing(1, 1, 1, DateTime.Now.AddDays(-5), DateTime.Now.AddDays(2)); // Student 1 Active
        var b2 = new Borrowing(2, 1, 3, DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-3)); // Student 1 Returned
        b2.MarkAsReturned(DateTime.Now.AddDays(-3));
        var b3 = new Borrowing(3, 2, 2, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(5)); // Student 2 Active

        await borrowingRepo.AddAsync(b1);
        await borrowingRepo.AddAsync(b2);
        await borrowingRepo.AddAsync(b3);

        // 1. Filter by Active status
        var activeOnly = (await historyService.SearchFilterSortAsync(null, BorrowingStatusFilter.Active, BorrowingSortField.CheckoutDate, true)).ToList();
        Assert.Equal(2, activeOnly.Count);
        Assert.All(activeOnly, b => Assert.Equal(BorrowingStatus.Active, b.Status));

        // 2. Filter by Returned status
        var returnedOnly = (await historyService.SearchFilterSortAsync(null, BorrowingStatusFilter.Returned, BorrowingSortField.CheckoutDate, true)).ToList();
        Assert.Single(returnedOnly);
        Assert.Equal(2, returnedOnly[0].Id);

        // 3. Sort by CheckoutDate Descending
        var sortedDesc = (await historyService.SearchFilterSortAsync(null, BorrowingStatusFilter.All, BorrowingSortField.CheckoutDate, true)).ToList();
        Assert.Equal(3, sortedDesc[0].Id); // Most recent checkout date (-2 days)
        Assert.Equal(2, sortedDesc[2].Id); // Oldest checkout date (-10 days)

        // 4. Sort by CheckoutDate Ascending
        var sortedAsc = (await historyService.SearchFilterSortAsync(null, BorrowingStatusFilter.All, BorrowingSortField.CheckoutDate, false)).ToList();
        Assert.Equal(2, sortedAsc[0].Id); // Oldest first (-10 days)
        Assert.Equal(3, sortedAsc[2].Id); // Most recent last (-2 days)
    }
}
