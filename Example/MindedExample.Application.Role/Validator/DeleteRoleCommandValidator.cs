using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Role.Command;
using MindedExample.Domain;

namespace MindedExample.Application.Role.Validator
{
    public class DeleteRoleCommandValidator : ICommandValidator<DeleteRoleCommand>
    {
        public Task<IValidationResult> ValidateAsync(DeleteRoleCommand command)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.RoleName))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.RoleName), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
            }
            else if (command.RoleName == Roles.Admin)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.RoleName), "The Admin role cannot be deleted", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            return Task.FromResult<IValidationResult>(validationResult);
        }
    }
}
