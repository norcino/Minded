using System.Threading.Tasks;
using Minded.Log;
using Microsoft.Extensions.Logging;
using Minded.Exception;
using Minded.CommandQuery.Query;

namespace Minded.Decorator.Exception
{
    public class ExceptionQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> _logger;

        public ExceptionQueryHandlerDecorator(IQueryHandler<TQuery, TResult> queryHandler, ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> logger) : base(queryHandler)   
        {
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query)
        {
            try
            {
                return await DecoratedQueryHandler.HandleAsync(query);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(LogEvent.QueryHandling, ex, query.ToLog().ToString());

                throw new QueryHandlerException<TQuery, TResult>("QueryHandlerException: " + query, ex, query);
            }
        }
    }
}
