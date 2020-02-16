using Minded.CommandQuery.Query;

namespace Minded.Decorator
{
    public class QueryHandlerDecoratorBase<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        public IQueryHandler<TQuery, TResult> InnerQueryHandler => DecoratedQueryHandler;

        protected readonly IQueryHandler<TQuery, TResult> DecoratedQueryHandler;

        public QueryHandlerDecoratorBase(IQueryHandler<TQuery, TResult> decoratedQueryHandler)
        {
            DecoratedQueryHandler = decoratedQueryHandler;
        }
    }
}
