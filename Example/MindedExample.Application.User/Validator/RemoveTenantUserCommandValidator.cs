using System.Linq;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;
using MindedExample.Domain;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validator for <see cref="RemoveTenantUserCommand"/>.
    /// Ensures the user exists in the current tenant, is not the legal owner, and that
    /// at least one admin will remain after the removal.
    /// </summary>
    public class RemoveTenantUserCommandValidator : ICommandValidator<RemoveTenantUserCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="RemoveTenantUserCommandValidator"/>.
        /// </summary>
        public RemoveTenantUserCommandValidator(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(RemoveTenantUserCommand command)
        {
            var result = new ValidationResult();

            if (!_currentUserAccessor.TenantId.HasValue)
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), "Tenant context is required", GenericErrorCodes.ValidationFailed, Severity.Error));
                return result;
            }

            var tenantId = _currentUserAccessor.TenantId.Value;

            var targetUser = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == command.UserId && u.TenantId == tenantId);

            if (targetUser == null)
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), "User with ID {0} not found in the current tenant", command.UserId,
                    Severity.Error, GenericErrorCodes.SubjectNotFound));
                return result;
            }

            var legalOwnerUserId = await _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.LegalOwnerUserId)
                .SingleOrDefaultAsync();

            if (targetUser.Id == legalOwnerUserId)
            {
                result.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), "The legal owner cannot be removed", GenericErrorCodes.ValidationFailed, Severity.Error));
                return result;
            }

            var removingAdmin = targetUser.TenantRole == TenantMemberRoles.Owner
                || targetUser.TenantRole == TenantMemberRoles.Admin;

            if (removingAdmin)
            {
                var adminCount = await _context.Users
                    .Where(u => u.TenantId == tenantId
                        && (u.TenantRole == TenantMemberRoles.Owner || u.TenantRole == TenantMemberRoles.Admin))
                    .CountAsync();

                if (adminCount <= 1)
                {
                    result.OutcomeEntries.Add(new OutcomeEntry(
                        nameof(command.UserId), "At least one tenant admin must remain",
                        GenericErrorCodes.ValidationFailed, Severity.Error));
                }
            }

            return result;
        }
    }
}
