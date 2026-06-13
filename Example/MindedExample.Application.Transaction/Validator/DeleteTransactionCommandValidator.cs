using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Transaction.Command;

namespace MindedExample.Application.Transaction.Validator
{
    /// <summary>
    /// Validator for DeleteTransactionCommand.
    /// Ensures the transaction exists before allowing the delete operation.
    /// Returns a 404 error code if the transaction is not found.
    /// </summary>
    public class DeleteTransactionCommandValidator : ICommandValidator<DeleteTransactionCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteTransactionCommandValidator(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Validates the delete command.
        /// Checks if the transaction exists within the caller's tenant. The check must be
        /// tenant-scoped: an unscoped check would answer differently for foreign-tenant ids
        /// and nonexistent ids, leaking which ids exist in other tenants.
        /// </summary>
        /// <param name="command">The delete command to validate</param>
        /// <returns>Validation result with 404 error code if transaction not found</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteTransactionCommand command)
        {
            var validationResult = new ValidationResult();

            // Check if the transaction exists in the caller's tenant
            var tenantId = _currentUserAccessor.TenantId;
            var exists = tenantId.HasValue && await _context.Transactions
                .AnyAsync(t => t.Id == command.TransactionId && t.User.TenantId == tenantId.Value);
            if (!exists)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TransactionId),
                    "Transaction with ID {0} not found", command.TransactionId, Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return validationResult;
        }
    }
}

