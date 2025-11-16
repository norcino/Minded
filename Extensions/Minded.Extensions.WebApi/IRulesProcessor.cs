using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Processor for command and query rules.
    /// </summary>
    public interface IRulesProcessor
    {
        IActionResult ProcessCommandRules(RestOperation operation, ICommandResponse result);
        IActionResult ProcessCommandRules<T>(RestOperation operation, ICommandResponse<T> result);
        IActionResult ProcessQueryRules(RestOperation operation, object result);
        IActionResult ProcessQueryRules<T>(RestOperation operation, IQueryResponse<T> result);
    }
}
