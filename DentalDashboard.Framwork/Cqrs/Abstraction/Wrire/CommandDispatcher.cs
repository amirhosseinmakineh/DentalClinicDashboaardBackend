using DentalDashboard.Framwork.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Framwork.Cqrs.Abstraction.Wrire
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IServiceProvider serviceProvider;

        public CommandDispatcher(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<Result> DispatchAsync(ICommand command,CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());

            dynamic handler = serviceProvider.GetRequiredService(handlerType);

            return await handler.HandleAsync((dynamic)command,cancellationToken);
        }

        public async Task<Result<TResponse>> DispatchAsync<TResponse>(ICommand<TResponse> command,CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(),typeof(TResponse));

            dynamic handler = serviceProvider.GetRequiredService(handlerType);

            return await handler.HandleAsync((dynamic)command,cancellationToken);
        }
    }
}