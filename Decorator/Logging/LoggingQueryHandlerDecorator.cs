using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Minded.CommandQuery.Query;
using Microsoft.Extensions.Logging;

namespace Minded.Decorator.Logging
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
            
            var originalLogInfo = query.ToLog();
            
            var formattedTime = string.Format("{0:mm\\:ss\\:fff}", stopWatch.Elapsed);
            var template = formattedTime + " {QueryName} - " + originalLogInfo.LogMessageTemplate;
            
            var properties = new List<object> { query.GetType().Name };
            properties.AddRange(originalLogInfo.LogMessageParameters);
            
            _logger.LogInformation(template, properties.ToArray());

            return response;
        }
    }
}