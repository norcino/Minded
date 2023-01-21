using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using System.Threading.Tasks;

namespace Minded.Extensions.WebApi
{
    public interface IRestMediator
    {
        Task<ActionResult> ProcessRestQueryAsync<TResult>(RestOperation operation, IQuery<TResult> query);

        Task<ActionResult> ProcessRestCommandAsync(RestOperation operation, ICommand command);

        Task<ActionResult> ProcessRestCommandAsync<TResult>(RestOperation operation, ICommand<TResult> command);
    }
}
