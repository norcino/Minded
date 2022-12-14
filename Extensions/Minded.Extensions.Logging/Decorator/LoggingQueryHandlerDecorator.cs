using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Logging.Decorator
{
    public class LoggingQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> _logger;

        public LoggingQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decoratedQueryHandler, ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> logger) : base(decoratedQueryHandler)
        {
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query)
        {
            var stopWatch = Stopwatch.StartNew();

            _logger.LogInformation(string.Format("{0} Started", query.GetType().Name));

            var response = await DecoratedQueryHandler.HandleAsync(query);
            stopWatch.Stop();

            var originalLogInfo = (query as ILoggableCommand)?.ToLog();

            var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
            var template = formattedTime + " {QueryName} - " + originalLogInfo?.LogMessageTemplate ?? query.GetType().Name;

            var properties = new List<object> { query.GetType().Name };

            if(originalLogInfo != null)
                properties.AddRange(originalLogInfo.LogMessageParameters);

            _logger.LogInformation(template, properties.ToArray());

            return response;
        }
    }
}
