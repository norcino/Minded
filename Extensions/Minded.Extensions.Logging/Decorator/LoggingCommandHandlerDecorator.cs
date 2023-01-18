using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Logging.Decorator
{
    public class LoggingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger _logger;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger logger) : base(commandHandler)
        {
            _logger = logger;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            var stopWatch = Stopwatch.StartNew();

            _logger.LogInformation("{Command} Started", command.GetType().Name);

            var response = await DecoratedCommmandHandler.HandleAsync(command);
            stopWatch.Stop();

            var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
            var properties = new List<object> { command.GetType().Name, response.Successful };
            var template = formattedTime + " {CommandName:l} {CommandSuccessful}";

            if (command is ILoggable)
            {
                var originalLogInfo = (command as ILoggable).ToLog();                
                template = $"{template} - {originalLogInfo.LogMessageTemplate}";                
                properties.AddRange(originalLogInfo.LogMessageParameters);            
            }
            
            _logger.LogInformation(template, properties.ToArray());
            
            return response;
        }
    }

    public class LoggingCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly ILogger _logger;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler, ILogger logger) : base(commandHandler)
        {
            _logger = logger;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command)
        {
            var stopWatch = Stopwatch.StartNew();

            _logger.LogInformation("{Command} Started", command.GetType().Name);

            var response = await DecoratedCommmandHandler.HandleAsync(command);
            stopWatch.Stop();

            var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
            var properties = new List<object> { command.GetType().Name, response.Successful };
            var template = formattedTime + " {CommandName:l} {CommandSuccessful}";

            if (command is ILoggable)
            {
                var originalLogInfo = (command as ILoggable).ToLog();
                template = $"{template} - {originalLogInfo.LogMessageTemplate}";
                properties.AddRange(originalLogInfo.LogMessageParameters);
            }

            _logger.LogInformation(template, properties.ToArray());

            return response;
        }
    }
}
