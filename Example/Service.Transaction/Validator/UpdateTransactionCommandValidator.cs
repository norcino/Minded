using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Service.Transaction.Command;
using Service.Transaction.Query;

namespace Service.Transaction.Validator
{
    /// <summary>
    /// Validator for UpdateTransactionCommand.
    /// Ensures the transaction exists before allowing the update operation.
    /// Returns a 404 error code if the transaction is not found.
    /// </summary>
    public class UpdateTransactionCommandValidator : ICommandValidator<UpdateTransactionCommand>
    {
        private readonly IMediator _mediator;
        private readonly IValidator<Data.Entity.Transaction> _transactionValidator;

        public UpdateTransactionCommandValidator(IValidator<Data.Entity.Transaction> transactionValidator, IMediator mediator)
        {
            _transactionValidator = transactionValidator;
            _mediator = mediator;
        }

        /// <summary>
        /// Validates the update command.
        /// Checks if the transaction exists and validates the transaction entity.
        /// </summary>
        /// <param name="command">The update command to validate</param>
        /// <returns>Validation result with 404 error code if transaction not found</returns>
        public async Task<IValidationResult> ValidateAsync(UpdateTransactionCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.Transaction == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction), "{0} is mandatory", null, Severity.Error, GenericErrorCodes.BadRequest));
                return validationResult;
            }

            if (command.TransactionId != command.Transaction.Id)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TransactionId), "Transaction ID in command does not match Transaction entity ID", command.TransactionId, Severity.Error, GenericErrorCodes.BadRequest));
                return validationResult;
            }

            if (!await _mediator.ProcessQueryAsync(new ExistsTransactionByIdQuery(command.TransactionId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TransactionId), 
                    "Transaction with ID {0} not found", command.TransactionId, Severity.Error, GenericErrorCodes.SubjectNotFound));
                return validationResult;
            }

            // Validate the transaction entity
            IValidationResult transactionValidationResult = await _transactionValidator.ValidateAsync(command.Transaction);
            return transactionValidationResult.Merge(validationResult);
        }
    }
}

