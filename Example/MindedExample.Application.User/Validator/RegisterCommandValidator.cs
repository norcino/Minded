using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates <see cref="RegisterCommand"/> before it reaches the handler.
    /// Ensures that all required fields are present and, for join-tenant mode,
    /// that a tenant name is supplied.
    /// </summary>
    public class RegisterCommandValidator : ICommandValidator<RegisterCommand>
    {
        private const string ModeJoinTenant = "join-tenant";

        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(RegisterCommand command)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.Name))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Name), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.Surname))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Surname), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.Email))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Email), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(command.Password))
                result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Password), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            var mode = string.IsNullOrWhiteSpace(command.Mode)
                ? string.Empty
                : command.Mode.Trim().ToLowerInvariant();

            if (mode == ModeJoinTenant && string.IsNullOrWhiteSpace(command.TenantName))
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TenantName),
                    "{0} is required when mode is join-tenant",
                    GenericErrorCodes.ValidationFailed,
                    Severity.Error));
            }

            return await Task.FromResult<IValidationResult>(result);
        }
    }
}
