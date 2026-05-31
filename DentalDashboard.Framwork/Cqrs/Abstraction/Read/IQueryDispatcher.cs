using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Framwork.Cqrs.Abstraction.Read
{
    public interface IQueryDispatcher
    {
        Task<Result<TResponse>> DispatchAsync<TResponse>(IQuery<TResponse> query);
    }
}