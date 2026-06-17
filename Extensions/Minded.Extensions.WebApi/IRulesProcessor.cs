using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Evaluates the registered <see cref="ICommandRestRule"/> and <see cref="IQueryRestRule"/> sets
    /// to determine the appropriate <see cref="IActionResult"/> for a given operation outcome.
    /// </summary>
    public interface IRulesProcessor
    {
        /// <summary>Evaluates command rules for a command response without a typed result.</summary>
        /// <param name="operation">The REST operation that produced the response.</param>
        /// <param name="result">The command response to evaluate.</param>
        /// <returns>The <see cref="IActionResult"/> dictated by the first matching rule.</returns>
        IActionResult ProcessCommandRules(RestOperation operation, ICommandResponse result);

        /// <summary>Evaluates command rules for a command response that carries a typed result.</summary>
        /// <typeparam name="T">The type of the command result.</typeparam>
        /// <param name="operation">The REST operation that produced the response.</param>
        /// <param name="result">The typed command response to evaluate.</param>
        /// <returns>The <see cref="IActionResult"/> dictated by the first matching rule.</returns>
        IActionResult ProcessCommandRules<T>(RestOperation operation, ICommandResponse<T> result);

        /// <summary>Evaluates query rules for a raw (untyped) query result.</summary>
        /// <param name="operation">The REST operation that produced the result.</param>
        /// <param name="result">The raw query result object to evaluate.</param>
        /// <returns>The <see cref="IActionResult"/> dictated by the first matching rule.</returns>
        IActionResult ProcessQueryRules(RestOperation operation, object result);

        /// <summary>Evaluates query rules for a typed query response.</summary>
        /// <typeparam name="T">The type of the query result.</typeparam>
        /// <param name="operation">The REST operation that produced the response.</param>
        /// <param name="result">The typed query response to evaluate.</param>
        /// <returns>The <see cref="IActionResult"/> dictated by the first matching rule.</returns>
        IActionResult ProcessQueryRules<T>(RestOperation operation, IQueryResponse<T> result);
    }
}
