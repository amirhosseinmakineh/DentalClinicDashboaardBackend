using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Framwork.Cqrs.Abstraction.Read
{
    public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<Result<TResponse>> HandleQueryAsync(TQuery query);
    }
}