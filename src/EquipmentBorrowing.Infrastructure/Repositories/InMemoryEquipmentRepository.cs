using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment = new()
    {
        new Equipment(1, "EQ-001", "Digital Multimeter", "Handheld digital multimeter", isAvailable: true),
        new Equipment(2, "EQ-002", "Oscilloscope", "Digital oscilloscope", isAvailable: false),
        new Equipment(3, "EQ-003", "Soldering Iron", "60W soldering iron", isAvailable: true)
    };

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = _equipment.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(item);
    }

    public Task<Equipment?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLower();
        var item = _equipment.FirstOrDefault(e => e.EquipmentIdentifier.ToLower() == normalized);
        return Task.FromResult(item);
    }

    public Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Equipment>>(_equipment.OrderBy(e => e.EquipmentIdentifier));
    }

    public Task<IEnumerable<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Equipment>>(_equipment.Where(e => e.IsAvailable).OrderBy(e => e.EquipmentIdentifier));
    }

    public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        _equipment.Add(equipment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        var index = _equipment.FindIndex(e => e.Id == equipment.Id);
        if (index >= 0)
        {
            _equipment[index] = equipment;
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsWithIdentifierAsync(string identifier, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLower();
        var exists = _equipment.Any(e => e.EquipmentIdentifier.ToLower() == normalized && (!excludeId.HasValue || e.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}