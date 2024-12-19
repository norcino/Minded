using System;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public interface ICommandRestRule: IMessageRestRule
    {
        Func<ICommandResponse, bool> RuleCondition { get; }
    }
}
