using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using System.Threading.Tasks;

namespace Minded.Extensions.WebApi
{
    public interface IRestMediator
    {
        /// <summary>
        /// Instantiates the IQueryHandler for the given query and executes it's Handle method
        /// The return value or error is processed by IRulesPRocessor to determine the correct ActionResult to return
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operation"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<ActionResult> ProcessRestQueryAsync<TResult>(RestOperation operation, IQuery<TResult> query);

        /// <summary>
        /// Instantiates the ICommandHandler for the given command and executes it's Handle method
        /// The return value or error is processed by IRulesPRocessor to determine the correct ActionResult to return
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<ActionResult> ProcessRestCommandAsync(RestOperation operation, ICommand command);

        /// <summary>
        /// Instantiates the ICommandHandler for the given command and executes it's Handle method
        /// The return value or error is processed by IRulesPRocessor to determine the correct ActionResult to return
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="operation"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<ActionResult> ProcessRestCommandAsync<TResult>(RestOperation operation, ICommand<TResult> command);
    }
}
