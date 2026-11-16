using System;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IUnitOfWork
{
    Task<IDbTransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IDbTransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
