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
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Logging.Decorator
{
    /// <summary>
    /// Decorator which logs the information about all commands being processed by the mediator.
    /// Through configuration it is possible to customize the output adding or removing details.
    /// </summary>
    /// <typeparam name="TCommand">Command currently processed</typeparam>
    public class LoggingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger _logger;
        private readonly IOptions<LoggingOptions> _options;
        private readonly IDataSanitizer _dataSanitizer;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger<LoggingCommandHandlerDecorator<TCommand>> logger, IOptions<LoggingOptions> options, IDataSanitizer dataSanitizer) : base(commandHandler)
        {
            _logger = logger;
            _options = options;
            _dataSanitizer = dataSanitizer;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (!_options.Value.GetEffectiveEnabled())
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

            var stopWatch = Stopwatch.StartNew();
            LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, _dataSanitizer, "- Started");

            try
            {
                ICommandResponse response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, _dataSanitizer,
                    "in {Duration:c} - Completed: {CommandSuccessful}",
                    new List<object> { stopWatch.Elapsed, response.Successful });

                // Log outcome entries if enabled
                LoggindCommandHandlerSharedMethods<TCommand>.LogOutcomeEntries(_logger, command, response, _options);

                return response;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, _dataSanitizer,
                    "in {Duration:c} - Failed: {ExceptionMessage}",
                    new List<object> { stopWatch.Elapsed, e.Message });

                throw;
            }
        }
    }

    /// <summary>
    /// Decorator which logs the information about all commands being processed by the mediator.
    /// Through configuration it is possible to customize the output adding or removing details.
    /// </summary>
    /// <typeparam name="TCommand">Command currently processed</typeparam>
    public class LoggingCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly ILogger _logger;
        private readonly IOptions<LoggingOptions> _options;
        private readonly IDataSanitizer _dataSanitizer;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler, ILogger<LoggingCommandHandlerDecorator<TCommand, TResult>> logger, IOptions<LoggingOptions> options, IDataSanitizer dataSanitizer) : base(commandHandler)
        {
            _logger = logger;
            _options = options;
            _dataSanitizer = dataSanitizer;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (!_options.Value.GetEffectiveEnabled())
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

            var stopWatch = Stopwatch.StartNew();
            LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, _dataSanitizer, "- Started");

            try
            {
                ICommandResponse<TResult> response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, _dataSanitizer,
                    "in {Duration:c} - Completed: {CommandSuccessful}",
                    new List<object> { stopWatch.Elapsed, response.Successful });

                // Log outcome entries if enabled
                LoggindCommandHandlerSharedMethods<TCommand>.LogOutcomeEntries(_logger, command, response, _options);

                return response;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, _dataSanitizer,
                    "in {Duration:c} - Failed: {ExceptionMessage}",
                    new List<object> { stopWatch.Elapsed, e.Message });

                throw;
            }
        }
    }

    internal static class LoggindCommandHandlerSharedMethods<TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// If the command supports advanced logging, the templated and properties will be extended with those defined in the command.
        /// Sensitive data marked with [Confidential] or [PII] attributes is sanitized based on the configured DataProtectionMode.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="command">Command instance currently logged</param>
        /// <param name="options">LoggingOptions with the current configuration</param>
        /// <param name="dataSanitizer">Data sanitizer for protecting sensitive information</param>
        /// <param name="defaultTemplate">Logging template with basic information</param>
        /// <param name="properties">List of parameters which will be logged and interpolated with the template</param>
        public static void Log(ILogger logger, TCommand command, IOptions<LoggingOptions> options, IDataSanitizer dataSanitizer, string defaultTemplate = "", List<object> properties = null)
        {
            var defaults = new List<object>();
            defaultTemplate = "[Tracking:{TraceId}] {CommandName:l} " + defaultTemplate;

            defaults.Add(command.TraceId);
            defaults.Add(command.GetType().Name);
            properties = properties ?? new List<object>();
            defaults.AddRange(properties);

            if (options.Value.GetEffectiveLogMessageTemplateData() && command is ILoggable)
            {
                var loggable = (command as ILoggable);
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
        /// Logs outcome entries from the command response if outcome logging is enabled.
        /// Filters outcome entries based on the configured minimum severity level.
        /// Each outcome entry is logged with its severity, message, error code, and property name.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="command">Command instance currently logged</param>
        /// <param name="response">Command response containing outcome entries</param>
        /// <param name="options">LoggingOptions with the current configuration</param>
        public static void LogOutcomeEntries(ILogger logger, TCommand command, ICommandResponse response, IOptions<LoggingOptions> options)
        {
            if (!options.Value.GetEffectiveLogOutcomeEntries() || response?.OutcomeEntries == null || !response.OutcomeEntries.Any())
                return;

            Severity minimumSeverity = options.Value.GetEffectiveMinimumSeverityLevel();

            // Filter outcome entries based on severity level
            // Severity enum: Error = 0, Warning = 1, Info = 2
            // We want to log entries with severity <= minimumSeverity (e.g., if min is Warning, log Error and Warning)
            var filteredEntries = response.OutcomeEntries
                .Where(entry => entry.Severity <= minimumSeverity)
                .ToList();

            if (!filteredEntries.Any())
                return;

            foreach (IOutcomeEntry entry in filteredEntries)
            {
                LogLevel logLevel = MapSeverityToLogLevel(entry.Severity);
                var template = "[Tracking:{TraceId}] {CommandName:l} - Outcome: [{Severity}] {Message} (Property: {PropertyName}, Code: {ErrorCode})";
                var parameters = new object[]
                {
                    command.TraceId,
                    command.GetType().Name,
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
