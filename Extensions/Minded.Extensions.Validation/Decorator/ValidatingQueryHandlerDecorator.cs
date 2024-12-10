using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Validation.Decorator
{
    internal static class QueryStaticHelper
    {
        internal const string ValidationFailureTemplate = "Validation Failure: {QueryValidatorName:l} Failures: {ValidationFailures:l}";
        internal const string DebugOutcomeLogTemplate = "Validation {validationSuccess} for {QueryValidatorName:l}";
        internal const string LogTemplate = "Validation started: {QueryValidatorName:l} - ";

        /// <summary>
        /// Determine if the Query requires validation
        /// </summary>
        /// <param name="query">Subject Query</param>
        /// <returns>True if the Query requires validation</returns>
        internal static bool IsValidatingQuery<TQuery, TResult>(IQuery<TResult> Query)
        {
            var validationRequired = TypeDescriptor.GetAttributes(Query)[typeof(ValidateQueryAttribute)] != null;

            if(validationRequired && !TypeHelper.IsInterfaceOrImplementation(typeof(IQueryResponse<>), typeof(TResult)))
                throw new InvalidOperationException("Query validation can be used only implementing 'IQuery<IQueryResponse<TResult>>'");

            return validationRequired;
        }
    }

    /// <summary>
    /// Decorator responsible to determine if the Query requires validation, checking if it has the <see cref="ValidateQueryAttribute"/>.
    /// If the validation does not fail it will invoke the next <see cref="IQueryHandler{TQuery, TResult}"/> registered implementation
    /// </summary>
    /// <typeparam name="TQuery">Generic type if the <see cref="IQuery"/> implementation handled by the handler currently decorated</typeparam>
    public class ValidatingQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IQueryValidator<TQuery, TResult> _queryValidator;
        private readonly ILogger _logger;

        public ValidatingQueryHandlerDecorator(IQueryHandler<TQuery, TResult> queryHandler, ILogger<ValidatingQueryHandlerDecorator<TQuery, TResult>> logger, IQueryValidator<TQuery, TResult> queryValidator) : base(queryHandler)
        {
            _queryValidator = queryValidator;
            _logger = logger;
        }

        /// <summary>
        /// Execute the Query asynchronously returning an instance of IQueryResponse
        /// </summary>
        /// <param name="Query">Subject Query</param>
        /// <returns>An instance of <see cref="IQueryResponse"/> representing the output of the Query</returns>
        public async Task<TResult> HandleAsync(TQuery Query)
        {
            if (!QueryStaticHelper.IsValidatingQuery<TQuery,TResult>(Query))
            {
                return await InnerQueryHandler.HandleAsync(Query);
            }

            _logger.LogDebug(QueryStaticHelper.LogTemplate, _queryValidator.GetType().Name);

            var valResult = await _queryValidator.ValidateAsync(Query);

            TResult result = default;

            if (valResult.IsValid)
            {
                result = await InnerQueryHandler.HandleAsync(Query);
                _logger.LogDebug(QueryStaticHelper.DebugOutcomeLogTemplate, valResult.IsValid, _queryValidator.GetType().Name);
            }
            else
            {
                _logger.LogInformation(QueryStaticHelper.ValidationFailureTemplate, _queryValidator.GetType().Name, valResult.OutcomeEntries.Select(e => e.Message).ToArray());
                return (TResult) valResult;
            }
            
            
            if (result == null)
            {
                // If here it means that result is of type IQueryResult, if null it means this has not been correctly handled in the inner decorator
                Type resultType = typeof(TResult).GetGenericArguments().FirstOrDefault();
                var queryResponseType = typeof(QueryResponse<>).MakeGenericType(resultType);
                result = (TResult) Activator.CreateInstance(queryResponseType);
                ((IMessageResponse)result).Successful = false;
            }

            ((IMessageResponse)result).OutcomeEntries = valResult.OutcomeEntries.ToList();

            return result;
        }
    }
}
