using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.Validator
{
    public class CreateRoleCommandValidator : ICommandValidator<CreateRoleCommand>
    {
        public Task<IValidationResult> ValidateAsync(CreateRoleCommand command)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.RoleName))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.RoleName), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            return Task.FromResult<IValidationResult>(validationResult);
        }
    }
}
