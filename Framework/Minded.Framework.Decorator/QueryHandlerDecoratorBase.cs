using Minded.Framework.CQRS.Query;

namespace Minded.Framework.Decorator
{
    /// <summary>
    /// Base class for query handler decorators.
    /// Provides access to the decorated query handler instance.
    /// </summary>
    /// <typeparam name="TQuery">Type of query being handled</typeparam>
    /// <typeparam name="TResult">Type of result returned by the query</typeparam>
    public class QueryHandlerDecoratorBase<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        public IQueryHandler<TQuery, TResult> InnerQueryHandler => DecoratedQueryHandler;

        protected readonly IQueryHandler<TQuery, TResult> DecoratedQueryHandler;

        public QueryHandlerDecoratorBase(IQueryHandler<TQuery, TResult> queryHandler)
        {
            DecoratedQueryHandler = queryHandler;
        }
    }
}
