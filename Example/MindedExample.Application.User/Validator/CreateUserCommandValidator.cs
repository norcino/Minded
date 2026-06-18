using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="CreateUserCommand"/> before it reaches the handler.
    /// Ensures the user payload is present, has no pre-assigned ID, and passes entity-level validation.
    /// </summary>
    public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
    {
        private readonly IValidator<MindedExample.Domain.User> _userValidator;

        public CreateUserCommandValidator(IValidator<MindedExample.Domain.User> userValidator)
        {
            _userValidator = userValidator;
        }

        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(CreateUserCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.User == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.User), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
                return validationResult;
            }

            if (command.User.Id != 0)
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.User.Id), "{0} should not be specified on creation", GenericErrorCodes.ValidationFailed, Severity.Error));

            return (await _userValidator.ValidateAsync(command.User)).Merge(validationResult);
        }
    }
}
