using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// REST-aware mediator that processes commands and queries and returns appropriate HTTP responses.
    /// Handles OperationCanceledException to return proper HTTP status codes for cancelled requests.
    /// </summary>
    public class RestMediator : Mediator, IRestMediator
    {
        private readonly IRulesProcessor _rulesProcessor;

        public RestMediator(IServiceProvider services, IRulesProcessor rulesProcessor) : base(services)
        {
            _rulesProcessor = rulesProcessor;
        }

        /// <summary>
        /// Processes a query and returns an appropriate HTTP response.
        /// If the operation is cancelled, returns HTTP 499 (Client Closed Request).
        /// </summary>
        public async Task<IActionResult> ProcessRestQueryAsync<TResult>(RestOperation operation, IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ProcessQueryAsync(query, cancellationToken);
                return _rulesProcessor.ProcessQueryRules(operation, result);
            }
            catch (OperationCanceledException)
            {
                // Return 499 Client Closed Request (nginx convention)
                // This indicates the client disconnected or the request was cancelled
                return new StatusCodeResult(499);
            }
        }

        /// <summary>
        /// Processes a command and returns an appropriate HTTP response.
        /// If the operation is cancelled, returns HTTP 499 (Client Closed Request).
        /// </summary>
        public async Task<IActionResult> ProcessRestCommandAsync(RestOperation operation, ICommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ProcessCommandAsync(command, cancellationToken);
                return _rulesProcessor.ProcessCommandRules(operation, result);
            }
            catch (OperationCanceledException)
            {
                // Return 499 Client Closed Request (nginx convention)
                // This indicates the client disconnected or the request was cancelled
                return new StatusCodeResult(499);
            }
        }

        /// <summary>
        /// Processes a command with a result and returns an appropriate HTTP response.
        /// If the operation is cancelled, returns HTTP 499 (Client Closed Request).
        /// </summary>
        public async Task<IActionResult> ProcessRestCommandAsync<TResult>(RestOperation operation, ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ProcessCommandAsync<TResult>(command, cancellationToken);
                return _rulesProcessor.ProcessCommandRules<TResult>(operation, result);
            }
            catch (OperationCanceledException)
            {
                // Return 499 Client Closed Request (nginx convention)
                // This indicates the client disconnected or the request was cancelled
                return new StatusCodeResult(499);
            }
        }
    }
}
