using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

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

        /// <inheritdoc cref="IMediator.ProcessQueryAsync{TResult}(IQuery{TResult}, CancellationToken)"/>
        public async Task<TResult> ProcessQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            dynamic handler = _services.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Unable to retrieve the handler for query: {handlerType.FullName}");

            return await handler.HandleAsync((dynamic)query, cancellationToken);
        }

        /// <inheritdoc cref="IMediator.ProcessCommandAsync(ICommand, CancellationToken)"/>
        public async Task<ICommandResponse> ProcessCommandAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _services.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Unable to retrieve the handler for command: {handlerType.FullName}");

            return await handler.HandleAsync((dynamic)command, cancellationToken);
        }

        /// <inheritdoc cref="IMediator.ProcessCommandAsync{TResult}(ICommand{TResult}, CancellationToken)"/>
        public async Task<ICommandResponse<TResult>> ProcessCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            var commandType = command.GetType();
            var resultType = typeof(TResult);
            Type[] typeArgs = { commandType, resultType };

            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(typeArgs);
            dynamic handler = _services.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Unable to retrieve the handler for command: {handlerType.FullName}");

            var result = await handler.HandleAsync((dynamic)command, cancellationToken);

            if (result != null)
                return result;

            var specialisedCommandResponse = (ICommandResponse<TResult>)Activator.CreateInstance(typeof(CommandResponse<TResult>));
            specialisedCommandResponse.OutcomeEntries = new List<IOutcomeEntry>
            {
                new OutcomeEntry("", "The handler returned a null result")
            };
            specialisedCommandResponse.Successful = false;

            return specialisedCommandResponse;
        }
    }
}
