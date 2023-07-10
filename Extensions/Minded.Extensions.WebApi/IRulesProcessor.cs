using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public interface IRulesProcessor
    {
        IActionResult ProcessCommandRules(RestOperation operation, ICommandResponse result);
        IActionResult ProcessCommandRules<T>(RestOperation operation, ICommandResponse<T> result);
        IActionResult ProcessQueryRules(RestOperation operation, object result);
    }
}
