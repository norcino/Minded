using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    public class DefaultRulesProcessor : IRulesProcessor
    {
        private readonly IRestRulesProvider _ruleProvider;

        public DefaultRulesProcessor(IRestRulesProvider ruleProvider)
        {
            _ruleProvider = ruleProvider;
        }

        private IQueryRestRule GetQueryRule(RestOperation operation, object result)
        {
            return _ruleProvider
               .GetQueryRules()
               .First(r => r.Operation == operation && (r.RuleCondition == null || r.RuleCondition(result)));
        }

        private ICommandRestRule GetCommandRule(RestOperation operation, ICommandResponse result)
        {
            return _ruleProvider
               .GetCommandRules()
               .First(r => r.Operation == operation && (r.RuleCondition == null || r.RuleCondition(result)));
        }

        public ActionResult ProcessQueryRules(RestOperation operation, object result)
        {
            var rule = GetQueryRule(operation, result);

            if (rule == null)
            {
                if(result == null)
                {
                    return new OkResult();
                }
                else
                {
                    return new OkObjectResult(result);
                }
            }

            if (rule.AddContent)
            {
                return new ObjectResult(result)
                {
                    StatusCode = (int)rule.ResultStatusCode
                };
            }

            return new StatusCodeResult((int)rule.ResultStatusCode);
        }

        public ActionResult ProcessCommandRules(RestOperation operation, ICommandResponse result)
        {
            var rule = GetCommandRule(operation, result);

            if (rule == null)
            {
                if (result == null)
                {
                    return new OkResult();
                }

                if (result.Successful)
                {
                    return new OkObjectResult(result);
                }
                else
                {
                    return new BadRequestObjectResult(result);
                }
            }

            if (rule.AddContent)
            {
                return new ObjectResult(result)
                {
                    StatusCode = (int)rule.ResultStatusCode
                };
            }

            return new StatusCodeResult((int)rule.ResultStatusCode);
        }

        public ActionResult ProcessCommandRules<T>(RestOperation operation, ICommandResponse<T> result)
        {
            var rule = GetCommandRule(operation, result);

            if (rule == null)
            {
                if (result == null)
                {
                    return new OkResult();
                }

                if(result.Successful)
                {
                    return new OkObjectResult(result);
                }
                else
                {
                    return new BadRequestObjectResult(result);
                }
            }

            if (rule.AddContent)
            {
                return new ObjectResult(result)
                {
                    StatusCode = (int)rule.ResultStatusCode
                };
            }

            return new StatusCodeResult((int)rule.ResultStatusCode);
        }
    }
}
