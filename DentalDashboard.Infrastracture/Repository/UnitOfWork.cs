using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly DentalContext _context;

    public UnitOfWork(DentalContext context)
    {
        _context = context;
    }

    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        await (_transaction ?? throw new InvalidOperationException("No active transaction.")).CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await (_transaction ?? throw new InvalidOperationException("No active transaction.")).RollbackAsync(cancellationToken);
    }
}
