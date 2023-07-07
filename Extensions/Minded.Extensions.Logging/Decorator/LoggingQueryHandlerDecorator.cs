using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Logging.Configuration;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Logging.Decorator
{
    public class LoggingQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly IOptions<LoggingOptions> _options;

        public LoggingQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decoratedQueryHandler, ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> logger, IOptions<LoggingOptions> options) : base(decoratedQueryHandler)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<TResult> HandleAsync(TQuery query)
        {
            if (!_options.Value.Enabled)
                return await DecoratedQueryHandler.HandleAsync(query);

            var stopWatch = Stopwatch.StartNew();
            Log(_logger, query, _options, "- Started");

            try
            {
                var response = await DecoratedQueryHandler.HandleAsync(query);
                stopWatch.Stop();
                Log(_logger, query, _options,
                    "in {Duration:c} - Completed",
                    new List<object> { stopWatch.Elapsed });

                return response;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                Log(_logger, query, _options,
                    "in {Duration:c} - Failed: {ExceptionMessage}",
                    new List<object> { stopWatch.Elapsed, e.Message });

                throw;
            }
        }

        /// <summary>
        /// If the query supports advanced logging, the templated and properties will be extended with thense defined in the query
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="query">Query instance currently logged</param>
        /// <param name="options">LoggingOptions with the current configuration</param>
        /// <param name="properties">List of parameters which will be logged and interpolated with the template</param>
        /// <param name="defaultTemplate">Logging template with basic information</param>
        private static void Log(ILogger logger, TQuery query, IOptions<LoggingOptions> options, string defaultTemplate = "", List<object> properties = null)
        {
            var defaults = new List<object>();
            defaultTemplate = "[Tracking:{TraceId}] {QueryName:l} " + defaultTemplate;

            defaults.Add(query.TraceId);
            defaults.Add(query.GetType().Name);
            properties = properties ?? new List<object>();
            defaults.AddRange(properties);

            if (options.Value.LogMessageTemplateData && query is ILoggable)
            {
                var loggable = (query as ILoggable);
                defaultTemplate += $" - {loggable.LoggingTemplate}";
                defaults.AddRange(loggable.LoggingParameters);
            }

            logger.LogInformation(defaultTemplate, defaults.ToArray());
        }
    }
}
