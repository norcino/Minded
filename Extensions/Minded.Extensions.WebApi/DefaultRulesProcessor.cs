using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// This class provides methods to process the rules necessary to determine which ActionResult to return
    /// </summary>
    public class DefaultRulesProcessor : IRulesProcessor
    {
        private readonly IRestRulesProvider _ruleProvider;

        public DefaultRulesProcessor(IRestRulesProvider ruleProvider)
        {
            _ruleProvider = ruleProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual IActionResult ProcessQueryRules(RestOperation operation, object result)
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
                    if (TypeHelper.IsInterfaceOrImplementation(typeof(IQueryResponse<>), result.GetType()))
                    {
                        if ((result as IQueryResponse<object>).Successful)
                            return new OkObjectResult((result as IQueryResponse<object>).Result);
                        else
                            return new StatusCodeResult((int)rule.ResultStatusCode);
                    }
                    return new OkObjectResult(result);
                }
            }

            if (rule.ContentResponse != ContentResponse.None)
            {
                if (result != null && TypeHelper.IsInterfaceOrImplementation(typeof(IQueryResponse<>), result.GetType()))
                {
                    return new ObjectResult((result as IQueryResponse<object>).Result)
                    {
                        StatusCode = (int)rule.ResultStatusCode
                    };
                }
                return new ObjectResult(result)
                {
                    StatusCode = (int)rule.ResultStatusCode
                };
            }

            return new StatusCodeResult((int)rule.ResultStatusCode);
        }

        /// <summary>
        /// Process a command rule for a given operation using the provided CommandResponse
        /// </summary>
        /// <param name="operation">API Operation</param>
        /// <param name="result">CommandResponse result of the proccessing of the command</param>
        /// <returns>ActionResult to be returned to the API</returns>
        public virtual IActionResult ProcessCommandRules(RestOperation operation, ICommandResponse result)
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

            if (rule.ContentResponse == ContentResponse.Result)
            {
                
                return new ObjectResult(result)
                {
                    StatusCode = (int)rule.ResultStatusCode
                };
            }

            return new StatusCodeResult((int)rule.ResultStatusCode);
        }

        /// <summary>
        /// Process a command rule for a given operation using the provided CommandResponse
        /// </summary>
        /// <typeparam name="T">Generic type of the CommandResponse</typeparam>
        /// <param name="operation">API Operation</param>
        /// <param name="result">CommandResponse result of the proccessing of the command</param>
        /// <returns>ActionResult to be returned to the API</returns>
        public virtual IActionResult ProcessCommandRules<T>(RestOperation operation, ICommandResponse<T> result)
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

            if (rule.ContentResponse != ContentResponse.None)
            {
                object resultObject;
                if(rule.ContentResponse == ContentResponse.Full)
                {
                    resultObject = result;
                }
                else
                {
                    resultObject = result.Result;
                }

                return new ObjectResult(resultObject)
                {
                    StatusCode = (int)rule.ResultStatusCode
                };
            }

            return new StatusCodeResult((int)rule.ResultStatusCode);
        }

        private IQueryRestRule GetQueryRule(RestOperation operation, object result)
        {
            return _ruleProvider?
               .GetQueryRules()?
               .FirstOrDefault(r => operation.Matches(r.Operation)
                    && (r.RuleCondition == null || r.RuleCondition(result))
               );
        }

        private ICommandRestRule GetCommandRule(RestOperation operation, ICommandResponse result)
        {            
            return _ruleProvider?
               .GetCommandRules()?
               .FirstOrDefault(r => operation.HasFlag(r.Operation) && (r.RuleCondition == null || r.RuleCondition(result)));
        }
    }
}
