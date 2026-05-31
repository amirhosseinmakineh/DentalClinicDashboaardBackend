using DentalDashboard.Framwork.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Framwork.Cqrs.Abstraction.Read
{
    public class QueryDispatcher : IQueryDispatcher
    {
        private readonly IServiceProvider serviceProvider;

        public QueryDispatcher(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<Result<TResponse>> DispatchAsync<TResponse>(IQuery<TResponse> query)
        {
            var queryType = query.GetType();

            var handlerType = typeof(IQueryHandler<,>)
                .MakeGenericType(queryType, typeof(TResponse));

            dynamic handler = serviceProvider.GetRequiredService(handlerType);

            return await handler.HandleQueryAsync((dynamic)query);
        }
    }
}