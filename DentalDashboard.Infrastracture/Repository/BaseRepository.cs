using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.IRepositories;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DentalDashboard.Infrastracture.Repository
{
    public class BaseRepository<TKey,TEntity> : IBaseRepository<TKey,TEntity>where TEntity : BaseEntity<TKey> where TKey : struct
    {
        protected readonly DentalContext context;

        protected readonly DbSet<TEntity> DbSet;

        public BaseRepository(DentalContext context)
        {
            this.context = context;
            DbSet = context.Set<TEntity>();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await DbSet.FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public async Task<PaginatedResult<TEntity>> GetPagedAsync(
            int pageNumber,
            int pageSize)
        {
            var totalCount = await DbSet.CountAsync();

            var items = await DbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet
                .Where(predicate)
                .ToListAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await DbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(
            IEnumerable<TEntity> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }

        public void Delete(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public async Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync()
        {
            return await DbSet.CountAsync();
        }

        public Task SaveChange()
        {
            return context.SaveChangesAsync();
        }
    }
}
