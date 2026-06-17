using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// REST-aware mediator that executes commands and queries via the underlying <see cref="Minded.Framework.Mediator.IMediator"/>
    /// and translates the result into the appropriate <see cref="IActionResult"/> using the registered
    /// <see cref="IRulesProcessor"/> and <see cref="IRestRulesProvider"/>.
    /// </summary>
    public interface IRestMediator
    {
        /// <summary>
        /// Instantiates the IQueryHandler for the given query and executes it's Handle method
        /// The return value or error is processed by IRulesPRocessor to determine the correct ActionResult to return
        /// </summary>
        /// <typeparam name="TResult">Result returned by the query</typeparam>
        /// <param name="operation">The REST operation context used to select the matching <see cref="IQueryRestRule"/>.</param>
        /// <param name="query">Query to be executed.</param>
        /// <param name="cancellationToken">Optional cancellation token. If not provided, a new CancellationToken will be used.</param>
        /// <returns>An <see cref="IActionResult"/> determined by the applicable REST rules.</returns>
        Task<IActionResult> ProcessRestQueryAsync<TResult>(RestOperation operation, IQuery<TResult> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates the ICommandHandler for the given command and executes it's Handle method
        /// The return value or error is processed by IRulesPRocessor to determine the correct ActionResult to return
        /// </summary>
        /// <param name="operation">The REST operation context used to select the matching <see cref="ICommandRestRule"/>.</param>
        /// <param name="command">Command to be executed.</param>
        /// <param name="cancellationToken">Optional cancellation token. If not provided, a new CancellationToken will be used.</param>
        /// <returns>An <see cref="IActionResult"/> determined by the applicable REST rules.</returns>
        Task<IActionResult> ProcessRestCommandAsync(RestOperation operation, ICommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates the ICommandHandler for the given command and executes it's Handle method
        /// The return value or error is processed by IRulesPRocessor to determine the correct ActionResult to return
        /// </summary>
        /// <typeparam name="TResult">Type of the expected command result</typeparam>
        /// <param name="operation">The REST operation context used to select the matching <see cref="ICommandRestRule"/>.</param>
        /// <param name="command">Command to be executed.</param>
        /// <param name="cancellationToken">Optional cancellation token. If not provided, a new CancellationToken will be used.</param>
        /// <returns>An <see cref="IActionResult"/> determined by the applicable REST rules.</returns>
        Task<IActionResult> ProcessRestCommandAsync<TResult>(RestOperation operation, ICommand<TResult> command, CancellationToken cancellationToken = default);
    }
}
