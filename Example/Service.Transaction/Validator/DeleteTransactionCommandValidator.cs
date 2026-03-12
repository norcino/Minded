using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Service.Transaction.Command;

namespace Service.Transaction.Validator
{
    /// <summary>
    /// Validator for DeleteTransactionCommand.
    /// Ensures the transaction exists before allowing the delete operation.
    /// Returns a 404 error code if the transaction is not found.
    /// </summary>
    public class DeleteTransactionCommandValidator : ICommandValidator<DeleteTransactionCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteTransactionCommandValidator(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates the delete command.
        /// Checks if the transaction exists in the database.
        /// </summary>
        /// <param name="command">The delete command to validate</param>
        /// <returns>Validation result with 404 error code if transaction not found</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteTransactionCommand command)
        {
            var validationResult = new ValidationResult();

            // Check if the category exists
            var exists = await _context.Transactions.AnyAsync(c => c.Id == command.TransactionId);
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

