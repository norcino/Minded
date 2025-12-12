using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Exception.Configuration;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Query;
using System.Collections.Generic;
using Minded.Framework.CQRS.Abstractions.Sanitization;

namespace Minded.Extensions.Exception.Decorator
{
    public class ExceptionQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly ILoggingSanitizerPipeline _sanitizerPipeline;
        private readonly IOptions<ExceptionOptions> _options;

        public ExceptionQueryHandlerDecorator(
            IQueryHandler<TQuery, TResult> queryHandler,
            ILogger<ExceptionQueryHandlerDecorator<TQuery, TResult>> logger,
            ILoggingSanitizerPipeline sanitizerPipeline,
            IOptions<ExceptionOptions> options) : base(queryHandler)
        {
            _logger = logger;
            _sanitizerPipeline = sanitizerPipeline;
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
                string queryInfo;

                // Check if serialization is enabled
                if (_options.Value.GetEffectiveSerialize())
                {
                    queryInfo = "Query serialization unavailable";

                    try
                    {
                        // Use the centralized sanitization pipeline
                        // This applies all registered sanitizers (diagnostic, data protection, property exclusions, etc.)
                        IDictionary<string, object> sanitizedQuery = _sanitizerPipeline.Sanitize(query);
                        queryInfo = JsonSerializer.Serialize(sanitizedQuery);
                    }
                    catch { }
                }
                else
                {
                    // Serialization disabled - just include the query type name
                    queryInfo = $"Type: {typeof(TQuery).Name} (serialization disabled)";
                }

                _logger.LogError(ex, ex.Message);

                throw new QueryHandlerException<TQuery, TResult>(query, "QueryHandlerException: " + queryInfo, ex);
            }
        }
    }
}
