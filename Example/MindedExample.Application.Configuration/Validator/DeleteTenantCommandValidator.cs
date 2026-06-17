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
    /// Validates tenant deletion requests.
    /// </summary>
    public class DeleteTenantCommandValidator : ICommandValidator<DeleteTenantCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteTenantCommandValidator(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<IValidationResult> ValidateAsync(DeleteTenantCommand command)
        {
            var validationResult = new ValidationResult();

            var tenant = await _context.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == command.TenantId);

            if (tenant == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TenantId),
                    "Tenant with ID {0} not found",
                    command.TenantId,
                    Severity.Error,
                    GenericErrorCodes.SubjectNotFound));

                return validationResult;
            }


            if (string.IsNullOrWhiteSpace(command.ConfirmationName) ||
                command.ConfirmationName.Trim() != tenant.Name)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.ConfirmationName),
                    "Tenant name confirmation does not match.",
                    command.ConfirmationName,
                    Severity.Error,
                    GenericErrorCodes.ValidationFailed));
            }

            return validationResult;
        }
    }

}
