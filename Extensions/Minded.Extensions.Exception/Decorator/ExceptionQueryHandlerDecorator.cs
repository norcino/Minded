using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Query;
using System.Collections.Generic;

namespace Minded.Extensions.Exception.Decorator
{
    public class ExceptionQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly IDataSanitizer _dataSanitizer;
        private readonly IOptions<DataProtectionOptions> _options;

        public ExceptionQueryHandlerDecorator(IQueryHandler<TQuery, TResult> queryHandler, ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> logger, IDataSanitizer dataSanitizer, IOptions<DataProtectionOptions> options) : base(queryHandler)
        {
            _logger = logger;
            _dataSanitizer = dataSanitizer;
            _options = options;
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
                string queryJson = "Query serialization unavailable";

                try
                {
                    // First, sanitize query to remove non-serializable types and excluded properties
                    IDictionary<string, object> diagnosticSanitized = DiagnosticDataSanitizer.Sanitize(query);

                    // Then apply data protection sanitization to protect sensitive data
                    IDictionary<string, object> sanitizedQuery = _dataSanitizer.Sanitize(diagnosticSanitized);
                    queryJson = JsonSerializer.Serialize(sanitizedQuery);
                }
                catch { }

                _logger.LogError(ex, ex.Message);

                throw new QueryHandlerException<TQuery, TResult>(query, "QueryHandlerException: " + queryJson, ex);
            }
        }
    }
}
