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
    /// Validator for DeleteUserCommand.
    /// Ensures the user exists before allowing the delete operation.
    /// Returns a 404 error code if the user is not found.
    /// </summary>
    public class DeleteUserCommandValidator : ICommandValidator<DeleteUserCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteUserCommandValidator(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Validates the delete command.
        /// Checks if the user exists within the caller's tenant. The check must be
        /// tenant-scoped: an unscoped check would answer differently for foreign-tenant ids
        /// and nonexistent ids, leaking which ids exist in other tenants.
        /// </summary>
        /// <param name="command">The delete command to validate</param>
        /// <returns>Validation result with 404 error code if user not found</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteUserCommand command)
        {
            var validationResult = new ValidationResult();

            // Check if the user exists in the caller's tenant
            var tenantId = _currentUserAccessor.TenantId;
            var exists = tenantId.HasValue && await _context.Users
                .AnyAsync(u => u.Id == command.UserId && u.TenantId == tenantId.Value);
            if (!exists)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), 
                    "User with ID {0} not found", command.UserId, Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return validationResult;
        }
    }
}

