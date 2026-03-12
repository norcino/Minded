using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Service.User.Query;

namespace Service.User.Validator
{
    /// <summary>
    /// Validator for GetUserByIdQuery.
    /// Currently no validation is required for this query.
    /// </summary>
    public class GetUserByIdQueryValidator : IQueryValidator<GetUserByIdQuery, Data.Entity.User>
    {
        public GetUserByIdQueryValidator()
        {
        }

        /// <summary>
        /// Validates the query.
        /// Currently returns a successful validation result.
        /// </summary>
        /// <param name="query">The query to validate</param>
        /// <returns>Validation result</returns>
        public async Task<IValidationResult> ValidateAsync(GetUserByIdQuery query)
        {
            var validationResult = new ValidationResult();
            return validationResult;
        }
    }
}

