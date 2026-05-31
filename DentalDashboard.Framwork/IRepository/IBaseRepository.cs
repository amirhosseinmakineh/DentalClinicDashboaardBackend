using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Domain;
using System.Linq.Expressions;

namespace DentalDashboard.Framwork.IRepositories
{
    public interface IBaseRepository<Tkey,TEntity>where TEntity : BaseEntity<Tkey> where Tkey : struct
    {
        Task<TEntity?> GetByIdAsync(Tkey id);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<PaginatedResult<TEntity>> GetPagedAsync(int pageNumber,int pageSize);

        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        Task AddAsync(TEntity entity);

        Task AddRangeAsync(IEnumerable<TEntity> entities);

        void Update(TEntity entity);

        void Delete(TEntity entity);

        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        Task<int> CountAsync();
        Task SaveChange();
    }
}
