using System;
using System.Net;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public class QueryRestRule : IQueryRestRule
    {
        public QueryRestRule(RestOperation operation, HttpStatusCode resultStatusCode, ContentResponse contentResponse, Func<object, bool> ruleCondition = null)
        {
            Operation = operation;
            ResultStatusCode = resultStatusCode;
            ContentResponse = contentResponse;
            RuleCondition = ruleCondition;
        }

        public RestOperation Operation { get; }

        public HttpStatusCode ResultStatusCode { get; }

        public ContentResponse ContentResponse { get; }

        public Func<object, bool> RuleCondition { get; }
    }

    public class CommandRestRule : ICommandRestRule
    {
        public CommandRestRule(RestOperation operation, HttpStatusCode resultStatusCode, ContentResponse contentResponse, Func<ICommandResponse, bool> ruleCondition = null)
        {
            Operation = operation;
            ResultStatusCode = resultStatusCode;
            ContentResponse = contentResponse;
            RuleCondition = ruleCondition;
        }

        public RestOperation Operation { get; }

        public HttpStatusCode ResultStatusCode { get; }

        public ContentResponse ContentResponse { get; }

        public Func<ICommandResponse, bool> RuleCondition { get; }
    }
}
