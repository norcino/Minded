using System.Threading.Tasks;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Framework.Mediator
{
    /// <summary>
    /// Mediator is responsible to take encapsulate the logic necessary to identify and invoke the
    /// specific handler which needs to be used to process given Queries and Commands
    /// </summary>
    public interface IMediator
    {
        /// <summary>
        /// Dynamically constructs the correct IQueryHandler for the given query and executes it's Handle method
        /// </summary>
        /// <typeparam name="TResult">Result returned by the query</typeparam>
        /// <param name="query">Query to be executed</param>
        /// <returns>Object or list of objects returned by the query</returns>
        Task<TResult> ProcessQueryAsync<TResult>(IQuery<TResult> query);

        /// <summary>
        /// Dynamically constructs the correct IQueryHandler for the given command and executes it's Handle method
        /// </summary>
        /// <param name="command">Command to be executed</param>
        /// <returns>ICommandResponse containing the information about the execution</returns>
        Task<ICommandResponse> ProcessCommandAsync(ICommand command);

        /// <summary>
        /// Dynamically constructs the correct IQueryHandler for the given command and executes it's Handle method
        /// </summary>
        /// <typeparam name="TResult">Type of the expected command result</typeparam>
        /// <param name="command">Command to be executed</param>
        /// <returns>ICommandResponse containing the result and information about the execution</returns>
        Task<ICommandResponse<TResult>> ProcessCommandAsync<TResult>(ICommand<TResult> command);
    }
}
