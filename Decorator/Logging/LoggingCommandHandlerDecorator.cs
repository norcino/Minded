using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Minded.Common;
using Microsoft.Extensions.Logging;

namespace Minded.Decorator.Logging
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
//            const string DurationMS = "DurationMS";
//            const string CommandSuccessful = "CommandSuccessful";

            var stopWatch = Stopwatch.StartNew();

            _logger.LogInformation(string.Format("{0} Started", command.GetType().Name));

            var response = await CommmandHandler.HandleAsync(command);
            stopWatch.Stop();

            var originalLogInfo = command.ToLog();

            var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
            var template = formattedTime + " {CommandName:l} - " + originalLogInfo.LogMessageTemplate;

            var properties = new List<object> { command.GetType().Name };
            properties.AddRange(originalLogInfo.LogMessageParameters);

//            var newLogInfo = new LogInfo(template, properties.ToArray());

//            var log = new Log(command.GetType().Name, newLogInfo);
//            log.AddContextProperty(DurationMS, stopWatch.ElapsedMilliseconds);
//            log.AddContextProperty(CommandSuccessful, response.Successful);
            _logger.LogInformation(template, properties.ToArray());

            return response;
        }
    }
}
