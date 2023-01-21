using System;
using System.Threading.Tasks;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using static System.Net.Mime.MediaTypeNames;

namespace Minded.Framework.Mediator
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
        public async Task<TResult> ProcessQueryAsync<TResult>(IQuery<TResult> query)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            dynamic handler = _services.GetService(handlerType);
            return await handler.HandleAsync((dynamic)query);
        }

        /// <inheritdoc cref="IMediator.ProcessCommandAsync(ICommand)"/>
        public async Task<ICommandResponse> ProcessCommandAsync(ICommand command)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _services.GetService(handlerType);
            return await handler.HandleAsync((dynamic)command);
        }

        /// <inheritdoc cref="IMediator.ProcessCommandAsync{TResult}(ICommand)"/>
        public async Task<ICommandResponse<TResult>> ProcessCommandAsync<TResult>(ICommand<TResult> command)
        {
            var commandType = command.GetType();
            var resultType = typeof(TResult);
            Type[] typeArgs = { commandType, resultType };

            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(typeArgs);
            dynamic handler = _services.GetService(handlerType);
            var result = await handler.HandleAsync((dynamic)command);

            if (result == null || result.GetType() == typeof(CommandResponse<TResult>))
            {
                return result;
            }
            
            var specialisedRommandResponse = (ICommandResponse<TResult>) Activator.CreateInstance(typeof(CommandResponse<TResult>));
            specialisedRommandResponse.OutcomeEntries = (result as ICommandResponse).OutcomeEntries;
            specialisedRommandResponse.Successful = (result as ICommandResponse).Successful;

            return specialisedRommandResponse;
        }
    }
}
