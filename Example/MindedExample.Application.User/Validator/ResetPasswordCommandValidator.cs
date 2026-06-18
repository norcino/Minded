using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="ResetPasswordCommand"/> before it reaches the handler.
    /// Ensures that the reset token and the new password are provided.
    /// </summary>
    public class ResetPasswordCommandValidator : ICommandValidator<ResetPasswordCommand>
    {
        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(ResetPasswordCommand command)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.Token))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Token), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.NewPassword))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.NewPassword), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            return await Task.FromResult<IValidationResult>(result);
        }
    }
}
