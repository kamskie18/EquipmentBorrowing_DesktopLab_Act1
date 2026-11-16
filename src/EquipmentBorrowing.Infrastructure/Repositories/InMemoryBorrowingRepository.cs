using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);
        return Task.CompletedTask;
    }

    public Task<int> CountActiveBorrowingsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var count = _borrowings.Count(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active);
        return Task.FromResult(count);
    }

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var borrowing = _borrowings.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(borrowing);
    }

    public Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        var index = _borrowings.FindIndex(b => b.Id == borrowing.Id);
        if (index >= 0)
        {
            _borrowings[index] = borrowing;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = _borrowings.Where(b => b.Status == BorrowingStatus.Active);
        return Task.FromResult<IEnumerable<Borrowing>>(active.ToList());
    }

    public Task<IEnumerable<Borrowing>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var items = _borrowings.Where(b => b.StudentId == studentId).OrderByDescending(b => b.DateBorrowed);
        return Task.FromResult<IEnumerable<Borrowing>>(items.ToList());
    }

    public Task<IEnumerable<Borrowing>> GetByEquipmentIdAsync(int equipmentId, CancellationToken cancellationToken = default)
    {
        var items = _borrowings.Where(b => b.EquipmentId == equipmentId).OrderByDescending(b => b.DateBorrowed);
        return Task.FromResult<IEnumerable<Borrowing>>(items.ToList());
    }

    public Task<IEnumerable<Borrowing>> SearchFilterSortAsync(
        string? searchText,
        BorrowingStatusFilter statusFilter,
        BorrowingSortField sortField,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = _borrowings.AsEnumerable();

        if (statusFilter == BorrowingStatusFilter.Active)
            query = query.Where(b => b.Status == BorrowingStatus.Active);
        else if (statusFilter == BorrowingStatusFilter.Returned)
            query = query.Where(b => b.Status == BorrowingStatus.Returned);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim().ToLower();
            query = query.Where(b =>
                (b.Student != null && b.Student.Name.ToLower().Contains(search)) ||
                (b.Student != null && b.Student.StudentNumber.ToLower().Contains(search)) ||
                (b.Equipment != null && b.Equipment.Name.ToLower().Contains(search)) ||
                (b.Equipment != null && b.Equipment.EquipmentIdentifier.ToLower().Contains(search)));
        }

        if (sortField == BorrowingSortField.CheckoutDate)
        {
            query = sortDescending
                ? query.OrderByDescending(b => b.DateBorrowed).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.DateBorrowed).ThenBy(b => b.Id);
        }
        else
        {
            query = sortDescending
                ? query.OrderByDescending(b => b.ExpectedReturnDate).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.ExpectedReturnDate).ThenBy(b => b.Id);
        }

        return Task.FromResult<IEnumerable<Borrowing>>(query.ToList());
    }
}