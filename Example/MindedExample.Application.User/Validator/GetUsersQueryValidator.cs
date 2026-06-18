using System.Collections.Generic;
using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="GetUsersQuery"/> before it reaches the handler.
    /// Ensures that the requested page size does not exceed the allowed maximum.
    /// </summary>
    public class GetUsersQueryValidator : IQueryValidator<GetUsersQuery, IQueryResponse<IEnumerable<MindedExample.Domain.User>>>
    {
        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(GetUsersQuery query)
        {
            var validationResult = new ValidationResult();

            if (query.Top > 100)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(query.Top), "{0} is above the maximum allowed 100", GenericErrorCodes.ValidationFailed, Severity.Error));
                return validationResult;
            }

            return await Task.FromResult<IValidationResult>(validationResult);
        }
    }
}
