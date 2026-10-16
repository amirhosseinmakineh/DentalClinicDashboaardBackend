//using DentalDashboard.Domain.Models;
//using DentalDashboard.Framwork.Application;
//using DentalDashboard.Framwork.Domain;
//using DentalDashboard.Framwork.IRepositories;

//namespace DentalDashboard.Framwork.Application
//{
//    public abstract class BaseCrudService<TKey,TEntity> : BaseService where TEntity : BaseEntity<TKey> where TKey : struct
//    {
//        protected readonly IBaseRepository<TKey,TEntity> _repository;

//        protected BaseCrudService(IBaseRepository<TKey,TEntity> repository)
//        {
//            _repository = repository;
//        }
//        public virtual async Task<Result<TEntity>> CreateAsync(TEntity entity)
//        {
//            return await _repository.AddAsync(entity);
//        }

//        public virtual async Task<Result> UpdateAsync(TEntity entity)
//        {
//            return await _repository.Update(entity);
//        }

//        public virtual async Task<Result> DeleteAsync(TEntity entity)
//        {
//            return await _repository.Delete(entity);
//        }

//        public virtual async Task<Result<TEntity>> GetByIdAsync(long id)
//        {
//            return await _repository.GetByIdAsync(id);
//        }

//        public virtual async Task<Result<IReadOnlyList<TEntity>>> GetAllAsync()
//        {
//            var result = await _repository.GetAllAsync();
//            return result.IsSuccess
//                ? Success(result.Data!.ToList(), "Success")
//                : Failure<IReadOnlyList<TEntity>>(result.Message);
//        }

//        public virtual async Task<Result<PaginatedResult<TEntity>>> GetPagedAsync(int pageNumber = 1, int pageSize = 10)
//        {
//            var allItemsResult = await _repository.GetAllAsync();
//            if (!allItemsResult.IsSuccess) return Failure<PaginatedResult<TEntity>>(allItemsResult.Message);

//            var items = allItemsResult.Data!
//                .Skip((pageNumber - 1) * pageSize)
//                .Take(pageSize)
//                .ToList();

//            var paged = new PaginatedResult<TEntity>
//            {
//                Items = items,
//                TotalCount = allItemsResult.Data!.Count(),
//                PageNumber = pageNumber,
//                PageSize = pageSize
//            };

//            return Success(paged);
//        }
//    }
//}