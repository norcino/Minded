using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="LoginCommand"/> before it reaches the handler.
    /// Ensures that email and password are provided.
    /// </summary>
    public class LoginCommandValidator : ICommandValidator<LoginCommand>
    {
        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(LoginCommand command)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.Email))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Email), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.Password))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Password), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            return await Task.FromResult<IValidationResult>(result);
        }
    }
}
