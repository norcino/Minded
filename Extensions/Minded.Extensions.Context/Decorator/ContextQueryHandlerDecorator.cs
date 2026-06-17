using System;
using System.Threading;
using System.Threading.Tasks;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Context.Decorator
{
    /// <summary>
    /// Decorator that creates, publishes and disposes the ambient <see cref="IMindedContext"/> for the
    /// outermost query call, and reuses the existing instance for nested invocations. When the nested
    /// query implements <see cref="ITraceable"/>, its <c>TraceId</c> is aligned with the ambient context
    /// id so that the whole flow shares the same correlation identifier.
    /// </summary>
    /// <typeparam name="TQuery">Query type being handled.</typeparam>
    /// <typeparam name="TResult">Result type returned by the query.</typeparam>
    public class ContextQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IMindedContextAccessor _accessor;
        private readonly IMediator _mediator;

        public ContextQueryHandlerDecorator(IQueryHandler<TQuery, TResult> queryHandler, IMindedContextAccessor accessor, IMediator mediator) : base(queryHandler)
        {
            _accessor = accessor;
            _mediator = mediator;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var accessor = (MindedContextAccessor)_accessor;
            var existing = accessor.InternalCurrent;

            if (existing == null)
            {
                var context = new MindedContext(query.TraceId, DateTimeOffset.UtcNow, cancellationToken, _mediator);
                accessor.InternalCurrent = context;
                try
                {
                    return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                }
                finally
                {
                    accessor.InternalCurrent = null;
                    context.Dispose();
                }
            }

            if (query is ITraceable traceable)
                traceable.TraceId = existing.TraceId;

            existing.IncrementDepth();
            try
            {
                return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
            }
            finally
            {
                existing.DecrementDepth();
            }
        }
    }
}
