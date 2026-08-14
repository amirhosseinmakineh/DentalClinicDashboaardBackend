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

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A database transaction is already active.");

        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction is null)
            throw new InvalidOperationException("No database transaction is active.");

        await _context.SaveChangesAsync();
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
