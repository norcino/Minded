using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="AcceptInviteCommand"/> before it reaches the handler.
    /// Ensures that the invite code/token, email, and password are provided.
    /// </summary>
    public class AcceptInviteCommandValidator : ICommandValidator<AcceptInviteCommand>
    {
        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(AcceptInviteCommand command)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.CodeOrToken))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.CodeOrToken), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.Email))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Email), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.Password))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Password), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            return await Task.FromResult<IValidationResult>(result);
        }
    }
}
