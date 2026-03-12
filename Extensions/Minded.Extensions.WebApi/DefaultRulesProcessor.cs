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


        public IActionResult ProcessQueryRules<T>(RestOperation operation, IQueryResponse<T> result)
        {
            IQueryRestRule rule = GetQueryRule(operation, result);

            if (rule == null)
            {
                // When no rule matches and result is null, return OkResult as default fallback
                if (result == null)
                {
                    return new OkResult();
                }

                // When no rule matches, return based on success status
                if (result.Successful)
                {
                    return new OkObjectResult(result.Result);
                }
                else
                {
                    return new BadRequestObjectResult(result.Result);
                }
            }

            if (rule.ContentResponse != ContentResponse.None)
            {
                object resultObject;
                if (rule.ContentResponse == ContentResponse.Full)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual IActionResult ProcessQueryRules(RestOperation operation, object result)
        {
            IQueryRestRule rule = GetQueryRule(operation, result);

            if (rule == null)
            {
                // When no rule matches and result is null, return OkResult as default fallback
                if(result == null)
                {
                    return new OkResult();
                }
                else
                {
                    // Handle IQueryResponse objects specially to extract the Result property
                    if (TypeHelper.IsInterfaceOrImplementation(typeof(IQueryResponse<>), result.GetType()))
                    {
                        var queryResponse = result as IQueryResponse<object>;
                        if (queryResponse.Successful)
                            return new OkObjectResult(queryResponse.Result);
                        else
                            return new BadRequestObjectResult(queryResponse.Result);
                    }
                    // For plain objects, return as-is
                    return new OkObjectResult(result);
                }
            }

            if (rule.ContentResponse != ContentResponse.None)
            {
                object resultObject;
                if (result != null && TypeHelper.IsInterfaceOrImplementation(typeof(IQueryResponse<>), result.GetType()))
                {
                    // For IQueryResponse objects, extract the Result property if ContentResponse.Result is specified
                    // Otherwise return the full IQueryResponse object
                    if (rule.ContentResponse == ContentResponse.Result)
                    {
                        resultObject = (result as IQueryResponse<object>).Result;
                    }
                    else
                    {
                        resultObject = result;
                    }
                }
                else
                {
                    resultObject = result;
                }

                return new ObjectResult(resultObject)
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
            ICommandRestRule rule = GetCommandRule(operation, result);

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

            if (rule.ContentResponse != ContentResponse.None)
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
            ICommandRestRule rule = GetCommandRule(operation, result);

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
