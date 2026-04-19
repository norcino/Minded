using System;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// A REST rule that evaluates a condition against the <see cref="ICommandResponse"/> produced by a command handler.
    /// When the condition is met (or when no condition is specified), the rule's HTTP status code and content strategy are applied.
    /// </summary>
    public interface ICommandRestRule : IMessageRestRule
    {
        /// <summary>
        /// Optional predicate evaluated against the <see cref="ICommandResponse"/>.
        /// When <c>null</c> the rule matches every response for its <see cref="IMessageRestRule.Operation"/>.
        /// </summary>
        Func<ICommandResponse, bool> RuleCondition { get; }
    }
}
