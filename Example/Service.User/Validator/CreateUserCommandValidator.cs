using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Service.User.Command;

namespace Service.User.Validator
{
    /// <summary>
    /// Validator for CreateUserCommand.
    /// Ensures the user data is valid before creation.
    /// </summary>
    public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
    {
        private readonly IValidator<Data.Entity.User> _userValidator;

        public CreateUserCommandValidator(IValidator<Data.Entity.User> userValidator)
        {
            _userValidator = userValidator;
        }

        /// <summary>
        /// Validates the create command.
        /// Checks if the user entity is provided and validates the user data.
        /// Ensures the user ID is not specified (should be auto-generated).
        /// </summary>
        /// <param name="command">The create command to validate</param>
        /// <returns>Validation result</returns>
        public async Task<IValidationResult> ValidateAsync(CreateUserCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.User == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.User), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

                return validationResult;
            }

            Task<IValidationResult> result = _userValidator.ValidateAsync(command.User);

            if (command.User.Id != 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.User.Id), "{0} should not be specified on creation", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            return (await result).Merge(validationResult);
        }
    }
}

