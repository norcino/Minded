using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;
using MindedExample.Domain;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validator for <see cref="UpdateTenantUserRoleCommand"/>.
    /// Ensures the role is provided and is one of the valid tenant member roles.
    /// </summary>
    public class UpdateTenantUserRoleCommandValidator : ICommandValidator<UpdateTenantUserRoleCommand>
    {
        /// <inheritdoc />
        public Task<IValidationResult> ValidateAsync(UpdateTenantUserRoleCommand command)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.Role))
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Role), "{0} is required", GenericErrorCodes.ValidationFailed, Severity.Error));
                return Task.FromResult<IValidationResult>(result);
            }

            var normalizedRole = command.Role.Trim();
            if (normalizedRole != TenantMemberRoles.Owner
                && normalizedRole != TenantMemberRoles.Admin
                && normalizedRole != TenantMemberRoles.Member)
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Role),
                    $"{{0}} must be one of: {TenantMemberRoles.Owner}, {TenantMemberRoles.Admin}, {TenantMemberRoles.Member}",
                    GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            return Task.FromResult<IValidationResult>(result);
        }
    }
}
