namespace DentalDashboard.Framwork.Cqrs.Abstraction.Read
{
    public interface IQueryDispatcher
    {
        Task<TResponse> DispatchAsync<TResponse>(
            IQuery<TResponse> query,
            CancellationToken cancellationToken = default);
    }
}