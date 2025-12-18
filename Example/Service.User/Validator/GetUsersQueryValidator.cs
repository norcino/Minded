using System.Collections.Generic;
using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Service.User.Query;

namespace Service.User.Validator
{
    /// <summary>
    /// Validator for GetUsersQuery.
    /// Ensures query parameters are within acceptable limits.
    /// </summary>
    public class GetUsersQueryValidator : IQueryValidator<GetUsersQuery, IQueryResponse<IEnumerable<Data.Entity.User>>>
    {
        public GetUsersQueryValidator()
        {
        }

        /// <summary>
        /// Validates the query.
        /// Ensures the Top parameter does not exceed the maximum allowed value of 100.
        /// </summary>
        /// <param name="query">The query to validate</param>
        /// <returns>Validation result</returns>
        public async Task<IValidationResult> ValidateAsync(GetUsersQuery query)
        {
            var validationResult = new ValidationResult();
            if(query.Top > 100)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(query.Top), "{0} is above the maximum allowed 100", GenericErrorCodes.ValidationFailed, Severity.Error));
                return validationResult;
            }
            return validationResult;
        }
    }
}

