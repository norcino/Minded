using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Logging.Configuration;
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

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger<LoggingCommandHandlerDecorator<TCommand>> logger, IOptions<LoggingOptions> options) : base(commandHandler)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            if (!_options.Value.Enabled)
                return await DecoratedCommmandHandler.HandleAsync(command);

            var stopWatch = Stopwatch.StartNew();
            LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, "- Started");

            try
            {
                var response = await DecoratedCommmandHandler.HandleAsync(command);
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options,
                    "in {Duration:c} - Completed: {CommandSuccessful}",
                    new List<object> { stopWatch.Elapsed, response.Successful });

                return response;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options,
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

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler, ILogger<LoggingCommandHandlerDecorator<TCommand, TResult>> logger, IOptions<LoggingOptions> options) : base(commandHandler)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command)
        {
            if (!_options.Value.Enabled)
                return await DecoratedCommmandHandler.HandleAsync(command);

            var stopWatch = Stopwatch.StartNew();
            LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options, "- Started");

            try
            {
                var response = await DecoratedCommmandHandler.HandleAsync(command);
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options,
                    "in {Duration:c} - Completed: {CommandSuccessful}",
                    new List<object> { stopWatch.Elapsed, response.Successful });

                return response;
            }
            catch (Exception e)
            {
                stopWatch.Stop();
                LoggindCommandHandlerSharedMethods<TCommand>.Log(_logger, command, _options,
                    "in {Duration:c} - Failed: {ExceptionMessage}",
                    new List<object> { stopWatch.Elapsed, e.Message });

                throw;
            }
        }
    }

    internal static class LoggindCommandHandlerSharedMethods<TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// If the command supports advanced logging, the templated and properties will be extended with thense defined in the command
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="command">Command instance currently logged</param>
        /// <param name="options">LoggingOptions with the current configuration</param>
        /// <param name="defaultTemplate">Logging template with basic information</param>
        /// <param name="properties">List of parameters which will be logged and interpolated with the template</param>
        public static void Log(ILogger logger, TCommand command, IOptions<LoggingOptions> options, string defaultTemplate = "", List<object> properties = null)
        {
            var defaults = new List<object>();
            defaultTemplate = "[Tracking:{TraceId}] {CommandName:l} " + defaultTemplate;

            defaults.Add(command.TraceId);
            defaults.Add(command.GetType().Name);
            properties = properties ?? new List<object>();
            defaults.AddRange(properties);

            if (options.Value.LogMessageTemplateData && command is ILoggable)
            {
                var loggable = (command as ILoggable);
                defaultTemplate += $" - {loggable.LoggingTemplate}";
                defaults.AddRange(loggable.LoggingParameters);
            }

            logger.LogInformation(defaultTemplate, defaults.ToArray());
        }
    }
}
