using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Logging.Configuration;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Logging.Decorator
{
    /// <summary>
    /// Decorator which logs the information about all queries being processed by the mediator.
    /// Through configuration it is possible to customize the output adding or removing details.
    /// Supports logging of outcome entries from QueryResponse objects.
    /// </summary>
    /// <typeparam name="TQuery">Query type</typeparam>
    /// <typeparam name="TResult">Query result type</typeparam>
    public class LoggingQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly IOptions<LoggingOptions> _options;
        private readonly IDataSanitizer _dataSanitizer;

        public LoggingQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decoratedQueryHandler, ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> logger, IOptions<LoggingOptions> options, IDataSanitizer dataSanitizer) : base(decoratedQueryHandler)
        {
            _logger = logger;
            _options = options;
            _dataSanitizer = dataSanitizer;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            if (!_options.Value.GetEffectiveEnabled())
                return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);

            var stopWatch = Stopwatch.StartNew();
            Log(_logger, query, _options, _dataSanitizer, "- Started");

            try
            {
                TResult response = await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                stopWatch.Stop();
                Log(_logger, query, _options, _dataSanitizer,
                    "in {Duration:c} - Completed",
                    new List<object> { stopWatch.Elapsed });

                // Log outcome entries if the response is a QueryResponse with outcome entries
                LogOutcomeEntries(_logger, query, response, _options);

                return response;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                Log(_logger, query, _options, _dataSanitizer,
                    "in {Duration:c} - Failed: {ExceptionMessage}",
                    new List<object> { stopWatch.Elapsed, e.Message });

                throw;
            }
        }

        /// <summary>
        /// If the query supports advanced logging, the templated and properties will be extended with those defined in the query.
        /// Sensitive data marked with [Confidential] or [PII] attributes is sanitized based on the configured DataProtectionMode.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="query">Query instance currently logged</param>
        /// <param name="options">LoggingOptions with the current configuration</param>
        /// <param name="dataSanitizer">Data sanitizer for protecting sensitive information</param>
        /// <param name="defaultTemplate">Logging template with basic information</param>
        /// <param name="properties">List of parameters which will be logged and interpolated with the template</param>
        private static void Log(ILogger logger, TQuery query, IOptions<LoggingOptions> options, IDataSanitizer dataSanitizer, string defaultTemplate = "", List<object> properties = null)
        {
            var defaults = new List<object>();
            defaultTemplate = "[Tracking:{TraceId}] {QueryName:l} " + defaultTemplate;

            defaults.Add(query.TraceId);
            defaults.Add(query.GetType().Name);
            properties = properties ?? new List<object>();
            defaults.AddRange(properties);

            if (options.Value.GetEffectiveLogMessageTemplateData() && query is ILoggable)
            {
                var loggable = (query as ILoggable);
                defaultTemplate += $" - {loggable.LoggingTemplate}";

                // Sanitize logging parameters to protect sensitive data
                var sanitizedParameters = SanitizeLoggingParameters(loggable.LoggingParameters, dataSanitizer);
                defaults.AddRange(sanitizedParameters);
            }

            logger.LogInformation(defaultTemplate, defaults.ToArray());
        }

        /// <summary>
        /// Sanitizes logging parameters by checking if they contain sensitive properties.
        /// Objects with [Confidential] or [PII] attributes are sanitized based on the configured DataProtectionMode.
        /// </summary>
        /// <param name="parameters">Original logging parameters</param>
        /// <param name="dataSanitizer">Data sanitizer for protecting sensitive information</param>
        /// <returns>Sanitized parameters safe for logging</returns>
        private static object[] SanitizeLoggingParameters(object[] parameters, IDataSanitizer dataSanitizer)
        {
            if (parameters == null || parameters.Length == 0)
                return parameters;

            var sanitized = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param == null)
                {
                    sanitized[i] = null;
                    continue;
                }

                Type type = param.GetType();

                // For primitive types and strings, no sanitization needed
                if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) ||
                    type == typeof(Guid) || type.IsEnum || type == typeof(decimal))
                {
                    sanitized[i] = param;
                }
                else
                {
                    // For complex objects, sanitize and convert to JSON for logging
                    IDictionary<string, object> sanitizedDict = dataSanitizer.Sanitize(param);
                    sanitized[i] = sanitizedDict != null ? JsonSerializer.Serialize(sanitizedDict) : param;
                }
            }

            return sanitized;
        }

        /// <summary>
        /// Logs outcome entries from the query response if outcome logging is enabled.
        /// Filters outcome entries based on the configured minimum severity level.
        /// Each outcome entry is logged with its severity, message, error code, and property name.
        /// Only works if TResult implements IQueryResponse or IMessageResponse.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="query">Query instance currently logged</param>
        /// <param name="response">Query response that may contain outcome entries</param>
        /// <param name="options">LoggingOptions with the current configuration</param>
        private static void LogOutcomeEntries(ILogger logger, TQuery query, TResult response, IOptions<LoggingOptions> options)
        {
            if (!options.Value.GetEffectiveLogOutcomeEntries() || response == null)
                return;

            // Check if the response implements IMessageResponse (which has OutcomeEntries)
            var messageResponse = response as IMessageResponse;
            if (messageResponse == null || messageResponse.OutcomeEntries == null || !messageResponse.OutcomeEntries.Any())
                return;

            Severity minimumSeverity = options.Value.GetEffectiveMinimumSeverityLevel();

            // Filter outcome entries based on severity level
            // Severity enum: Error = 0, Warning = 1, Info = 2
            // We want to log entries with severity <= minimumSeverity (e.g., if min is Warning, log Error and Warning)
            var filteredEntries = messageResponse.OutcomeEntries
                .Where(entry => entry.Severity <= minimumSeverity)
                .ToList();

            if (!filteredEntries.Any())
                return;

            foreach (IOutcomeEntry entry in filteredEntries)
            {
                LogLevel logLevel = MapSeverityToLogLevel(entry.Severity);
                var template = "[Tracking:{TraceId}] {QueryName:l} - Outcome: [{Severity}] {Message} (Property: {PropertyName}, Code: {ErrorCode})";
                var parameters = new object[]
                {
                    query.TraceId,
                    query.GetType().Name,
                    entry.Severity,
                    entry.Message,
                    entry.PropertyName ?? "N/A",
                    entry.ErrorCode ?? "N/A"
                };

                logger.Log(logLevel, template, parameters);
            }
        }

        /// <summary>
        /// Maps outcome entry severity to Microsoft.Extensions.Logging.LogLevel.
        /// Error -> LogLevel.Error, Warning -> LogLevel.Warning, Info -> LogLevel.Information
        /// </summary>
        /// <param name="severity">Outcome entry severity</param>
        /// <returns>Corresponding LogLevel</returns>
        private static LogLevel MapSeverityToLogLevel(Severity severity)
        {
            if (severity == Severity.Error)
                return LogLevel.Error;
            if (severity == Severity.Warning)
                return LogLevel.Warning;
            return LogLevel.Information;
        }
    }
}
