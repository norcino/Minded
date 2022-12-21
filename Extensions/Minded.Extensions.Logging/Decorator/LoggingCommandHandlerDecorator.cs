using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Decorator;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Logging.Decorator
{
    public class LoggingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ILogger _logger;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger logger)
            : base(commandHandler)
        {
            _logger = logger;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            var stopWatch = Stopwatch.StartNew();

            _logger.LogInformation(string.Format("{0} Started", command.GetType().Name));

            var response = await DecoratedCommmandHandler.HandleAsync(command);
            stopWatch.Stop();

            if(command is ILoggable)
            {
                var originalLogInfo = (command as ILoggable).ToLog();

                var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
                var template = formattedTime + " {CommandName:l} {CommandSuccessful} - " + originalLogInfo.LogMessageTemplate;
                var properties = new List<object> { command.GetType().Name, response.Successful };
                properties.AddRange(originalLogInfo.LogMessageParameters);

                _logger.LogInformation(template, properties.ToArray());
            }
            else
            {
                var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
                var template = formattedTime + " {CommandName:l} {CommandSuccessful}";
                var properties = new List<object> { command.GetType().Name, response.Successful };
                _logger.LogInformation(template, properties.ToArray());
            }
            return response;
        }
    }
}
