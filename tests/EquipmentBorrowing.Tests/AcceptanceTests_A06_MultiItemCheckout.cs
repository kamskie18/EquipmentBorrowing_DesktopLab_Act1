using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class TestUnitOfWork : IUnitOfWork
{
    public bool RolledBack { get; private set; }
    public bool Committed { get; private set; }

    public Task<IDbTransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IDbTransactionScope>(new TestDbTransactionScope(this));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    private class TestDbTransactionScope : IDbTransactionScope
    {
        private readonly TestUnitOfWork _uow;

        public TestDbTransactionScope(TestUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _uow.Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            _uow.RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public class AcceptanceTests_A06_MultiItemCheckout
{
    [Fact]
    public async Task A06_UC06_CheckOutMultiple_RejectsFewerThanTwoItems()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        var result = await service.CheckOutMultipleAsync(1, new[] { 1 }, DateTime.Now.AddDays(7));

        Assert.False(result.IsSuccess);
        Assert.Contains("at least two", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(uow.Committed);
    }

    [Fact]
    public async Task A06_UC06_CheckOutMultiple_RejectsDuplicateItems()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        var result = await service.CheckOutMultipleAsync(1, new[] { 1, 1 }, DateTime.Now.AddDays(7));

        Assert.False(result.IsSuccess);
        Assert.Contains("duplicate", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(uow.Committed);
    }

    [Fact]
    public async Task A06_UC06_CheckOutMultiple_RejectsInvalidReturnDate()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        var result = await service.CheckOutMultipleAsync(1, new[] { 1, 3 }, DateTime.Now.AddDays(-1));

        Assert.False(result.IsSuccess);
        Assert.Contains("date", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A06_UC06_CheckOutMultiple_RejectsIneligibleStudent()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        // Student 2 is restricted
        var result = await service.CheckOutMultipleAsync(2, new[] { 1, 3 }, DateTime.Now.AddDays(7));

        Assert.False(result.IsSuccess);
        Assert.Contains("restricted", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(uow.RolledBack);
    }

    [Fact]
    public async Task A06_UC06_CheckOutMultiple_RejectsUnavailableItem()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        // Item 2 is unavailable in InMemoryEquipmentRepository
        var result = await service.CheckOutMultipleAsync(1, new[] { 1, 2 }, DateTime.Now.AddDays(7));

        Assert.False(result.IsSuccess);
        Assert.Contains("unavailable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(uow.RolledBack);
    }

    [Fact]
    public async Task A06_UC06_CheckOutMultiple_RejectsOverLimit_AndSucceedsExactlyAtLimit()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        // Setup extra available items
        await equipRepo.AddAsync(new Equipment(4, "EQ-004", "Power Supply", null, true));
        await equipRepo.AddAsync(new Equipment(5, "EQ-005", "Logic Analyzer", null, true));

        // Student 1 has 0 active. Request 2 items (EQ 1, 3) -> 2 <= 3 -> SUCCESS (Exactly at limit will be 1 + 2 = 3)
        // Let's first add 1 active borrowing for Student 1
        await borrowingRepo.AddAsync(new Borrowing(101, 1, 4, DateTime.Now, DateTime.Now.AddDays(7)));

        // Student 1 now has 1 active borrowing.
        // If student requests 3 items: 1 + 3 = 4 > 3 -> Must reject
        var overLimitResult = await service.CheckOutMultipleAsync(1, new[] { 1, 3, 5 }, DateTime.Now.AddDays(7));
        Assert.False(overLimitResult.IsSuccess);
        Assert.Contains("limit", overLimitResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // If student requests 2 items: 1 + 2 = 3 <= 3 -> Exactly at limit -> MUST SUCCEED!
        var atLimitResult = await service.CheckOutMultipleAsync(1, new[] { 1, 3 }, DateTime.Now.AddDays(7));
        Assert.True(atLimitResult.IsSuccess);
        Assert.Equal(2, atLimitResult.Borrowings.Count);
        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task A06_UC06_CheckOutMultiple_SimulateFailureAfterWrite_RollsBackCleanly()
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();
        var uow = new TestUnitOfWork();
        var service = new MultiItemBorrowService(studentRepo, equipRepo, borrowingRepo, uow);

        // Intentionally trigger failure seam after the first item write
        var result = await service.CheckOutMultipleAsync(
            studentId: 1,
            equipmentIds: new[] { 1, 3 },
            expectedReturnDate: DateTime.Now.AddDays(7),
            simulateFailureAfterFirstWrite: true);

        Assert.False(result.IsSuccess);
        Assert.Contains("rolled back cleanly", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(uow.RolledBack);
        Assert.False(uow.Committed);
    }
}
