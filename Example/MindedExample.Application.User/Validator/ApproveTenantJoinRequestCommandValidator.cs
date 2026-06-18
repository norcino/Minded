using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validator for <see cref="ApproveTenantJoinRequestCommand"/>.
    /// Ensures the join request exists in the current tenant and has not yet been processed.
    /// </summary>
    public class ApproveTenantJoinRequestCommandValidator : ICommandValidator<ApproveTenantJoinRequestCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="ApproveTenantJoinRequestCommandValidator"/>.
        /// </summary>
        public ApproveTenantJoinRequestCommandValidator(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(ApproveTenantJoinRequestCommand command)
        {
            var result = new ValidationResult();

            if (!_currentUserAccessor.TenantId.HasValue)
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.RequestId), "Tenant context is required", GenericErrorCodes.ValidationFailed, Severity.Error));
                return result;
            }

            var exists = await _context.TenantJoinRequests
                .AnyAsync(r => r.Id == command.RequestId
                    && r.TenantId == _currentUserAccessor.TenantId.Value
                    && r.ProcessedAtUtc == null);

            if (!exists)
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.RequestId),
                    "Pending join request with ID {0} not found", command.RequestId,
                    Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return result;
        }
    }
}
