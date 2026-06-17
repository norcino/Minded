using System.Linq;
using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Role.Command;
using MindedExample.Domain;

namespace MindedExample.Application.Role.Validator
{
    public class UpdateRolePermissionsCommandValidator : ICommandValidator<UpdateRolePermissionsCommand>
    {
        public Task<IValidationResult> ValidateAsync(UpdateRolePermissionsCommand command)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.RoleName))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.RoleName), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            if (command.PermissionNames == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.PermissionNames), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            if (command.RoleName == Roles.Admin && command.PermissionNames != null)
            {
                var missingProtected = Permissions.ProtectedAdminPermissions
                    .Where(p => !command.PermissionNames.Contains(p))
                    .ToList();

                if (missingProtected.Count > 0)
                {
                    validationResult.OutcomeEntries.Add(new OutcomeEntry(
                        nameof(command.PermissionNames),
                        $"Cannot remove protected permissions from Admin role: {string.Join(", ", missingProtected)}",
                        GenericErrorCodes.ValidationFailed,
                        Severity.Error));
                }
            }

            return Task.FromResult<IValidationResult>(validationResult);
        }
    }
}
