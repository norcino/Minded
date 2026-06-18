using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="GetInviteDetailsQuery"/> before it reaches the handler.
    /// Ensures that the invite token or code is provided.
    /// </summary>
    public class GetInviteDetailsQueryValidator : IQueryValidator<GetInviteDetailsQuery, InviteDetailsResult>
    {
        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(GetInviteDetailsQuery query)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(query.TokenOrCode))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(query.TokenOrCode), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            return await Task.FromResult<IValidationResult>(result);
        }
    }
}
