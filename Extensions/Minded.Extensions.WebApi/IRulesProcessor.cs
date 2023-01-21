using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public interface IRulesProcessor
    {
        ActionResult ProcessCommandRules(RestOperation operation, ICommandResponse result);
        ActionResult ProcessCommandRules<T>(RestOperation operation, ICommandResponse<T> result);
        ActionResult ProcessQueryRules(RestOperation operation, object result);
    }
}