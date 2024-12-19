using System;

namespace Minded.Extensions.WebApi
{
    public interface IQueryRestRule: IMessageRestRule
    {
        Func<object, bool> RuleCondition { get; }
    }
}
