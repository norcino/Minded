using System;
using System.Net;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public interface ICommandRestRule
    {
        RestOperation Operation { get; }
        HttpStatusCode ResultStatusCode { get; }
        bool AddContent { get; }
        Func<ICommandResponse, bool> RuleCondition { get; }
    }
}
