using System.Linq.Expressions;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Infrastructure.Repositories;

public abstract class AccountantRepositoryBase<TKey, TEntity>(DbContext context)
    : IBaseRepository<TKey, TEntity>
    where TEntity : BaseEntity<TKey>
    where TKey : struct
{
    protected DbSet<TEntity> Entities { get; } = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TKey id) =>
        await Entities.FindAsync(id);

    public async Task<IEnumerable<TEntity>> GetAllAsync() =>
        await Entities.ToListAsync();

    public IQueryable<TEntity> GetAll() => Entities;

    public async Task<PaginatedResult<TEntity>> GetPagedAsync(
        int pageNumber, int pageSize)
    {
        var totalCount = await Entities.CountAsync();
        var items = await Entities.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize).ToListAsync();
        return new PaginatedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate) =>
        await Entities.Where(predicate).ToListAsync();

    public async Task AddAsync(TEntity entity) =>
        await Entities.AddAsync(entity);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities) =>
        await Entities.AddRangeAsync(entities);

    public void Update(TEntity entity) => Entities.Update(entity);
    public void Delete(TEntity entity) => Entities.Remove(entity);

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate) =>
        await Entities.AnyAsync(predicate);

    public async Task<int> CountAsync() => await Entities.CountAsync();
    public Task SaveChange() => context.SaveChangesAsync();
}
