using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.Validator
{
    public class AssignRolesToUserCommandValidator : ICommandValidator<AssignRolesToUserCommand>
    {
        public Task<IValidationResult> ValidateAsync(AssignRolesToUserCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.UserId <= 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.UserId), "{0} must be a positive integer", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            if (command.RoleNames == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.RoleNames), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            return Task.FromResult<IValidationResult>(validationResult);
        }
    }
}
