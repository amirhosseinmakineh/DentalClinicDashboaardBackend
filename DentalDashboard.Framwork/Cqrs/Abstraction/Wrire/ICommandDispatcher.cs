using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Framwork.Cqrs.Abstraction.Wrire
{
    public interface ICommandDispatcher
    {
        Task<Result> DispatchAsync(ICommand command,CancellationToken cancellationToken = default);

        Task<Result<TResponse>> DispatchAsync<TResponse>(ICommand<TResponse> command,CancellationToken cancellationToken = default);
    }
}