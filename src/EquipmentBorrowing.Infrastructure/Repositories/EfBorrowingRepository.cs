using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingRepository : IBorrowingRepository
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfBorrowingRepository(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        await _context.Borrowings.AddAsync(borrowing, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountActiveBorrowingsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .CountAsync(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, cancellationToken);
    }

    public async Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _context.Borrowings.Update(borrowing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.Status == BorrowingStatus.Active)
            .OrderByDescending(b => b.DateBorrowed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Borrowing>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Equipment)
            .Include(b => b.Student)
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.DateBorrowed)
            .ThenByDescending(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Borrowing>> GetByEquipmentIdAsync(int equipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.EquipmentId == equipmentId)
            .OrderByDescending(b => b.DateBorrowed)
            .ThenByDescending(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Borrowing>> SearchFilterSortAsync(
        string? searchText,
        BorrowingStatusFilter statusFilter,
        BorrowingSortField sortField,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .AsQueryable();

        // 1. Status Filter
        if (statusFilter == BorrowingStatusFilter.Active)
        {
            query = query.Where(b => b.Status == BorrowingStatus.Active);
        }
        else if (statusFilter == BorrowingStatusFilter.Returned)
        {
            query = query.Where(b => b.Status == BorrowingStatus.Returned);
        }

        // 2. Partial Search (case-insensitive, trimmed)
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim().ToLower();
            query = query.Where(b =>
                (b.Student != null && b.Student.Name.ToLower().Contains(search)) ||
                (b.Student != null && b.Student.StudentNumber.ToLower().Contains(search)) ||
                (b.Equipment != null && b.Equipment.Name.ToLower().Contains(search)) ||
                (b.Equipment != null && b.Equipment.EquipmentIdentifier.ToLower().Contains(search)));
        }

        // 3. Sorting with stable tie-breaker ID (BR-17)
        if (sortField == BorrowingSortField.CheckoutDate)
        {
            query = sortDescending
                ? query.OrderByDescending(b => b.DateBorrowed).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.DateBorrowed).ThenBy(b => b.Id);
        }
        else // ExpectedReturnDate
        {
            query = sortDescending
                ? query.OrderByDescending(b => b.ExpectedReturnDate).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.ExpectedReturnDate).ThenBy(b => b.Id);
        }

        return await query.ToListAsync(cancellationToken);
    }
}