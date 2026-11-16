using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = new()
    {
        new Student(1, "S1001", "Juan Dela Cruz", "juan@campus.edu", isAllowedToBorrow: true),
        new Student(2, "S1002", "Maria Santos", "maria@campus.edu", isAllowedToBorrow: false),
        new Student(3, "S1003", "Pedro Reyes", null, isAllowedToBorrow: true)
    };

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = _students.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(student);
    }

    public Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default)
    {
        var normalized = studentNumber.Trim().ToLower();
        var student = _students.FirstOrDefault(s => s.StudentNumber.ToLower() == normalized);
        return Task.FromResult(student);
    }

    public Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Student>>(_students.OrderBy(s => s.Name));
    }

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        _students.Add(student);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        var index = _students.FindIndex(s => s.Id == student.Id);
        if (index >= 0)
        {
            _students[index] = student;
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsWithStudentNumberAsync(string studentNumber, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = studentNumber.Trim().ToLower();
        var exists = _students.Any(s => s.StudentNumber.ToLower() == normalized && (!excludeId.HasValue || s.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}