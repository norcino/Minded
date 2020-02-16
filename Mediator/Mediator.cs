using Minded.CommandQuery.Query;
using Minded.Common;
using System;
using System.Threading.Tasks;

namespace Minded.Mediator
{
    /// <inheritdoc cref="IMediator"/>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _services;

        public Mediator(IServiceProvider services)
        {
            _services = services;
        }

        /// <inheritdoc cref="IMediator.ProcessQueryAsync{TResult}(IQuery{TResult})"/>
        public Task<TResult> ProcessQueryAsync<TResult>(IQuery<TResult> query)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            dynamic handler = _services.GetService(handlerType);
            return handler.HandleAsync((dynamic)query);
        }

        /// <inheritdoc cref="IMediator.ProcessCommandAsync(ICommand)"/>
        public async Task<ICommandResponse> ProcessCommandAsync(ICommand command)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _services.GetService(handlerType);
            return await handler.HandleAsync((dynamic)command); 
        }

        /// <inheritdoc cref="IMediator.ProcessCommandAsync{TResult}(ICommand)"/>
        public async Task<ICommandResponse<TResult>> ProcessCommandAsync<TResult>(ICommand command)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _services.GetService(handlerType);

            var result = await handler.HandleAsync((dynamic)command);

            if (result == null || result.GetType() == typeof(CommandResponse<TResult>))
            {
                return result;
            }
            
            var specialisedRommandResponse = (ICommandResponse<TResult>) Activator.CreateInstance(typeof(CommandResponse<TResult>));
            specialisedRommandResponse.ValidationEntries = (result as ICommandResponse).ValidationEntries;
            specialisedRommandResponse.Successful = (result as ICommandResponse).Successful;

            return specialisedRommandResponse;
        }
    }
}
