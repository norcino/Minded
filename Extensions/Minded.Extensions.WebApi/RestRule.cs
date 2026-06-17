using System;
using System.Net;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Default implementation of <see cref="IQueryRestRule"/> that maps a query result to an HTTP response.
    /// </summary>
    public class QueryRestRule : IQueryRestRule
    {
        /// <summary>
        /// Initializes a new <see cref="QueryRestRule"/>.
        /// </summary>
        /// <param name="operation">The REST operation this rule applies to.</param>
        /// <param name="resultStatusCode">HTTP status code returned when this rule matches.</param>
        /// <param name="contentResponse">Content strategy for the response body.</param>
        /// <param name="ruleCondition">
        /// Optional predicate evaluated against the raw query result.
        /// When <c>null</c> the rule matches every result for its operation.
        /// </param>
        public QueryRestRule(RestOperation operation, HttpStatusCode resultStatusCode, ContentResponse contentResponse, Func<object, bool> ruleCondition = null)
        {
            Operation = operation;
            ResultStatusCode = resultStatusCode;
            ContentResponse = contentResponse;
            RuleCondition = ruleCondition;
        }

        /// <inheritdoc/>
        public RestOperation Operation { get; }

        /// <inheritdoc/>
        public HttpStatusCode ResultStatusCode { get; }

        /// <inheritdoc/>
        public ContentResponse ContentResponse { get; }

        /// <inheritdoc/>
        public Func<object, bool> RuleCondition { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="ICommandRestRule"/> that maps a command response to an HTTP response.
    /// </summary>
    public class CommandRestRule : ICommandRestRule
    {
        /// <summary>
        /// Initializes a new <see cref="CommandRestRule"/>.
        /// </summary>
        /// <param name="operation">The REST operation this rule applies to.</param>
        /// <param name="resultStatusCode">HTTP status code returned when this rule matches.</param>
        /// <param name="contentResponse">Content strategy for the response body.</param>
        /// <param name="ruleCondition">
        /// Optional predicate evaluated against the <see cref="ICommandResponse"/>.
        /// When <c>null</c> the rule matches every response for its operation.
        /// </param>
        public CommandRestRule(RestOperation operation, HttpStatusCode resultStatusCode, ContentResponse contentResponse, Func<ICommandResponse, bool> ruleCondition = null)
        {
            Operation = operation;
            ResultStatusCode = resultStatusCode;
            ContentResponse = contentResponse;
            RuleCondition = ruleCondition;
        }

        /// <inheritdoc/>
        public RestOperation Operation { get; }

        /// <inheritdoc/>
        public HttpStatusCode ResultStatusCode { get; }

        /// <inheritdoc/>
        public ContentResponse ContentResponse { get; }

        /// <inheritdoc/>
        public Func<ICommandResponse, bool> RuleCondition { get; }
    }
}
