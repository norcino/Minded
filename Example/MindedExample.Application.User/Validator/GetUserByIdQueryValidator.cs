using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="GetUserByIdQuery"/> before it reaches the handler.
    /// Ensures the requested user ID is a positive integer.
    /// </summary>
    public class GetUserByIdQueryValidator : IQueryValidator<GetUserByIdQuery, MindedExample.Domain.User>
    {
        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(GetUserByIdQuery query)
        {
            var result = new ValidationResult();

            if (query.UserId <= 0)
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(query.UserId), "{0} must be greater than zero", GenericErrorCodes.ValidationFailed, Severity.Error));

            return await Task.FromResult<IValidationResult>(result);
        }
    }
}
