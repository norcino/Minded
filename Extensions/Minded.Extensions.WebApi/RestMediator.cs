using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;

namespace Minded.Extensions.WebApi
{
    public class RestMediator : Mediator, IRestMediator
    {
        private readonly IRulesProcessor _rulesProcessor;

        public RestMediator(IServiceProvider services, IRulesProcessor rulesProcessor) : base(services)
        {
            _rulesProcessor = rulesProcessor;
        }

        public async Task<IActionResult> ProcessRestQueryAsync<TResult>(RestOperation operation, IQuery<TResult> query)
        {            
            var result = await ProcessQueryAsync(query);
            return _rulesProcessor.ProcessQueryRules(operation, result);
        }

        public async Task<IActionResult> ProcessRestCommandAsync(RestOperation operation, ICommand command)
        {
            var result = await ProcessCommandAsync(command);
            return _rulesProcessor.ProcessCommandRules(operation, result);
        }

        public async Task<IActionResult> ProcessRestCommandAsync<TResult>(RestOperation operation, ICommand<TResult> command)
        {
            var result = await ProcessCommandAsync<TResult>(command);
            return _rulesProcessor.ProcessCommandRules(operation, result);
        }
    }
}
