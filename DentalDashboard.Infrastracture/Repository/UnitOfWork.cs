using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;


using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly DentalContext _context;

    public UnitOfWork(DentalContext context)
    {
        _context = context;
    }

    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already active for this unit of work.");

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _transaction ?? throw new InvalidOperationException("No transaction is active.");
        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        var transaction = _transaction;
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is null)
            return;

        var transaction = _transaction;
        _transaction = null;
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    public void Dispose()
    {
        if (_transaction is null)
            return;

        var transaction = _transaction;
        _transaction = null;
        try
        {
            transaction.Rollback();
        }
        finally
        {
            transaction.Dispose();
        }
    }
}
