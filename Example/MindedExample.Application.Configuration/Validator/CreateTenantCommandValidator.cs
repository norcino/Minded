using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Configuration.Command;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.Configuration.Validator
{
    /// <summary>
    /// Validates tenant creation requests.
    /// </summary>
    public class CreateTenantCommandValidator : ICommandValidator<CreateTenantCommand>
    {
        private readonly IMindedExampleContext _context;

        public CreateTenantCommandValidator(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<IValidationResult> ValidateAsync(CreateTenantCommand command)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Name),
                    "{0} is mandatory",
                    command.Name,
                    Severity.Error,
                    GenericErrorCodes.ValidationFailed));
                return validationResult;
            }


            if (string.IsNullOrWhiteSpace(command.LegalOwnerEmail) || string.IsNullOrWhiteSpace(command.LegalOwnerPassword))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.LegalOwnerEmail),
                    "Legal owner credentials are required",
                    GenericErrorCodes.ValidationFailed,
                    Severity.Error));
                return validationResult;
            }

            var tenantName = command.Name.Trim();
            var ownerEmail = command.LegalOwnerEmail.Trim().ToLowerInvariant();

            if (await _context.Tenants.AnyAsync(t => t.Name == tenantName))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Name),
                    "A tenant with this name already exists.",
                    tenantName,
                    Severity.Error,
                    GenericErrorCodes.ValidationFailed));
            }

            if (await _context.Users.AnyAsync(u => u.Email == ownerEmail))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.LegalOwnerEmail),
                    "A user with this legal owner email already exists.",
                    ownerEmail,
                    Severity.Error,
                    GenericErrorCodes.ValidationFailed));
            }

            return validationResult;
        }
    }

}
