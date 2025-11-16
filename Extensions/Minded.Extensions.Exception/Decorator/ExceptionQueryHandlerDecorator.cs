using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Exception.Decorator
{
    public class ExceptionQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> _logger;

        public ExceptionQueryHandlerDecorator(IQueryHandler<TQuery, TResult> queryHandler, ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> logger) : base(queryHandler)
        {
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled (client disconnect, timeout, etc.)
                // This is not an error - just log as information and re-throw
                _logger.LogInformation("Query {QueryType} was cancelled", typeof(TQuery).Name);
                throw; // Re-throw to let ASP.NET Core or RestMediator handle it
            }
            catch (System.Exception ex)
            {
                var queryJson = "Query serialization unavailable";

                try
                {
                    queryJson = JsonSerializer.Serialize(query);
                }
                catch { }

                _logger.LogError(ex, ex.Message);

                throw new QueryHandlerException<TQuery, TResult>(query, "QueryHandlerException: " + queryJson, ex);
            }
        }
    }
}
