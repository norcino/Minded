using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using MindedExample.Application.Transaction.Command;
using MindedExample.Application.Transaction.Query;
using MindedExample.Application.Category.Query;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.Transaction.Validator
{
    /// <summary>
    /// Validator for UpdateTransactionCommand.
    /// Ensures the transaction exists before allowing the update operation.
    /// Returns a 404 error code if the transaction is not found.
    /// </summary>
    public class UpdateTransactionCommandValidator : ICommandValidator<UpdateTransactionCommand>
    {
        private readonly IMediator _mediator;
        private readonly IValidator<MindedExample.Domain.Transaction> _transactionValidator;

        public UpdateTransactionCommandValidator(IValidator<MindedExample.Domain.Transaction> transactionValidator, IMediator mediator)
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
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction), "{0} is mandatory", null, Severity.Error, GenericErrorCodes.ValidationFailed));
                return validationResult;
            }

            if (command.TransactionId != command.Transaction.Id)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TransactionId), "Transaction ID in command does not match Transaction entity ID", command.TransactionId, Severity.Error, GenericErrorCodes.ValidationFailed));
                return validationResult;
            }

            if (!await _mediator.ProcessQueryAsync(new ExistsTransactionByIdQuery(command.TransactionId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TransactionId),
                    "Transaction with ID {0} not found", command.TransactionId, Severity.Error, GenericErrorCodes.SubjectNotFound));
                return validationResult;
            }

            if (command.Transaction.CategoryId != 0 && !await _mediator.ProcessQueryAsync(new ExistsCategoryInCurrentTenantQuery(command.Transaction.CategoryId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Transaction.CategoryId),
                    "Category with ID {0} does not exist", command.Transaction.CategoryId, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            if (command.Transaction.UserId != 0 && !await _mediator.ProcessQueryAsync(new ExistsUserInCurrentTenantQuery(command.Transaction.UserId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Transaction.UserId),
                    "User with ID {0} does not exist in the current tenant", command.Transaction.UserId, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            IValidationResult transactionValidationResult = await _transactionValidator.ValidateAsync(command.Transaction);
            return transactionValidationResult.Merge(validationResult);
        }
    }
}

