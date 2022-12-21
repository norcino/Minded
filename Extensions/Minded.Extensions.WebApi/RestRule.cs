using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public class QueryRestRule : IQueryRestRule
    {
        public QueryRestRule(RestOperation operation, HttpStatusCode resultStatusCode, bool addContent, Func<object, bool> ruleCondition = null)
        {
            Operation = operation;
            ResultStatusCode = resultStatusCode;
            AddContent = addContent;
            RuleCondition = ruleCondition;
        }

        public RestOperation Operation { get; }

        public HttpStatusCode ResultStatusCode { get; }

        public bool AddContent { get; }

        public Func<object, bool> RuleCondition { get; }
    }

    public class CommandRestRule : ICommandRestRule
    {
        public CommandRestRule(RestOperation operation, HttpStatusCode resultStatusCode, bool addContent, Func<ICommandResponse, bool> ruleCondition = null)
        {
            Operation = operation;
            ResultStatusCode = resultStatusCode;
            AddContent = addContent;
            RuleCondition = ruleCondition;
        }

        public RestOperation Operation { get; }

        public HttpStatusCode ResultStatusCode { get; }

        public bool AddContent { get; }

        public Func<ICommandResponse, bool> RuleCondition { get; }
    }
}
