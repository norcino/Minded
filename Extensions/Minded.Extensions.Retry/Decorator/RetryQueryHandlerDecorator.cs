using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Retry.Configuration;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Retry.Decorator
{
    /// <summary>
    /// Decorator which implements retry logic for queries.
    /// Retries query execution based on RetryQueryAttribute configuration or default options.
    /// Can be configured to apply to all queries by default via RetryOptions.ApplyToAllQueries.
    /// </summary>
    /// <typeparam name="TQuery">Query type being handled</typeparam>
    /// <typeparam name="TResult">Result type returned by the query</typeparam>
    public class RetryQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<RetryQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly IOptions<RetryOptions> _options;

        /// <summary>
        /// Initializes a new instance of the RetryQueryHandlerDecorator class.
        /// </summary>
        /// <param name="decoratedQueryHandler">The decorated query handler</param>
        /// <param name="logger">Logger instance for logging retry attempts</param>
        /// <param name="options">Retry configuration options</param>
        public RetryQueryHandlerDecorator(
            IQueryHandler<TQuery, TResult> decoratedQueryHandler,
            ILogger<RetryQueryHandlerDecorator<TQuery, TResult>> logger,
            IOptions<RetryOptions> options) : base(decoratedQueryHandler)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// Handles the query with retry logic if the query has the RetryQueryAttribute
        /// or if RetryOptions.ApplyToAllQueries is true.
        /// </summary>
        /// <param name="query">The query to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query result</returns>
        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var attribute = (RetryQueryAttribute)TypeDescriptor.GetAttributes(query)[typeof(RetryQueryAttribute)];

            // If the query doesn't have the RetryQueryAttribute and ApplyToAllQueries is false, just execute it normally
            if (attribute == null && !_options.Value.ApplyToAllQueries)
            {
                return await InnerQueryHandler.HandleAsync(query, cancellationToken);
            }

            // Determine retry count and delays
            int retryCount;
            if (attribute != null && attribute.RetryCount.HasValue)
            {
                retryCount = attribute.RetryCount.Value;
            }
            else
            {
                retryCount = _options.Value.DefaultRetryCount;
            }

            var attempt = 0;
            Exception lastException = null;

            while (attempt <= retryCount)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[Tracking:{TraceId}] {QueryName:l} - Retry attempt {Attempt} of {MaxRetries}",
                            query.TraceId,
                            typeof(TQuery).Name,
                            attempt,
                            retryCount);
                    }

                    TResult result = await InnerQueryHandler.HandleAsync(query, cancellationToken);

                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[Tracking:{TraceId}] {QueryName:l} - Retry attempt {Attempt} succeeded",
                            query.TraceId,
                            typeof(TQuery).Name,
                            attempt);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt > retryCount)
                    {
                        _logger.LogError(
                            ex,
                            "[Tracking:{TraceId}] {QueryName:l} - All retry attempts exhausted ({MaxRetries}). Throwing exception.",
                            query.TraceId,
                            typeof(TQuery).Name,
                            retryCount);
                        throw;
                    }

                    // Get delay from attribute if present, otherwise use default
                    int delay = 0;
                    if (attribute != null)
                    {
                        delay = attribute.GetDelayForIteration(attempt);
                    }
                    
                    if (delay == 0)
                    {
                        delay = _options.Value.GetDefaultDelayForIteration(attempt);
                    }

                    _logger.LogWarning(
                        ex,
                        "[Tracking:{TraceId}] {QueryName:l} - Attempt {Attempt} failed. Retrying in {Delay}ms...",
                        query.TraceId,
                        typeof(TQuery).Name,
                        attempt,
                        delay);

                    if (delay > 0)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            // This should never be reached, but just in case
            throw lastException ?? new InvalidOperationException("Retry logic failed unexpectedly");
        }
    }
}

