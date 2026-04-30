using System;
using System.Threading;
using System.Threading.Tasks;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Context.Decorator
{
    /// <summary>
    /// Decorator that creates, publishes and disposes the ambient <see cref="IMindedContext"/> for the
    /// outermost command call, and reuses the existing instance for nested invocations. When the nested
    /// command implements <see cref="ITraceable"/>, its <c>TraceId</c> is aligned with the ambient
    /// context id so that the whole flow shares the same correlation identifier.
    /// </summary>
    /// <typeparam name="TCommand">Command type being handled.</typeparam>
    public class ContextCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly IMindedContextAccessor _accessor;
        private readonly IMediator _mediator;

        public ContextCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, IMindedContextAccessor accessor, IMediator mediator) : base(commandHandler)
        {
            _accessor = accessor;
            _mediator = mediator;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var accessor = (MindedContextAccessor)_accessor;
            var existing = accessor.InternalCurrent;

            if (existing == null)
            {
                var context = new MindedContext(command.TraceId, DateTimeOffset.UtcNow, cancellationToken, _mediator);
                accessor.InternalCurrent = context;
                try
                {
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }
                finally
                {
                    accessor.InternalCurrent = null;
                    context.Dispose();
                }
            }

            if (command is ITraceable traceable)
                traceable.TraceId = existing.TraceId;

            existing.IncrementDepth();
            try
            {
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
            finally
            {
                existing.DecrementDepth();
            }
        }
    }

    /// <summary>
    /// Decorator that creates, publishes and disposes the ambient <see cref="IMindedContext"/> for the
    /// outermost command call returning a typed result, and reuses the existing instance for nested
    /// invocations.
    /// </summary>
    /// <typeparam name="TCommand">Command type being handled.</typeparam>
    /// <typeparam name="TResult">Result type returned by the command.</typeparam>
    public class ContextCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly IMindedContextAccessor _accessor;
        private readonly IMediator _mediator;

        public ContextCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler, IMindedContextAccessor accessor, IMediator mediator) : base(commandHandler)
        {
            _accessor = accessor;
            _mediator = mediator;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var accessor = (MindedContextAccessor)_accessor;
            var existing = accessor.InternalCurrent;

            if (existing == null)
            {
                var context = new MindedContext(command.TraceId, DateTimeOffset.UtcNow, cancellationToken, _mediator);
                accessor.InternalCurrent = context;
                try
                {
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }
                finally
                {
                    accessor.InternalCurrent = null;
                    context.Dispose();
                }
            }

            if (command is ITraceable traceable)
                traceable.TraceId = existing.TraceId;

            existing.IncrementDepth();
            try
            {
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
            finally
            {
                existing.DecrementDepth();
            }
        }
    }
}
