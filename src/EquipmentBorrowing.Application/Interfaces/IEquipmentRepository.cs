using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
    public async Task<IEnumerable<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Equipment.AsNoTracking().Where(e => e.IsAvailable).ToListAsync(cancellationToken);
    }

}



