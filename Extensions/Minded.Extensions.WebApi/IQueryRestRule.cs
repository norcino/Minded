using System;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// A REST rule that evaluates a condition against the raw result object produced by a query handler.
    /// When the condition is met (or when no condition is specified), the rule's HTTP status code and content strategy are applied.
    /// </summary>
    public interface IQueryRestRule : IMessageRestRule
    {
        /// <summary>
        /// Optional predicate evaluated against the query result.
        /// When <c>null</c> the rule matches every result for its <see cref="IMessageRestRule.Operation"/>.
        /// </summary>
        Func<object, bool> RuleCondition { get; }
    }
}
