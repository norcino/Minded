using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using MindedExample.Application.Transaction.Command;
using MindedExample.Application.Transaction.Query;

namespace MindedExample.Application.Transaction.Validator
{
    /// <summary>
    /// Validator for <see cref="DeleteTransactionCommand"/>.
    /// Ensures the transaction exists (within the caller's tenant) before allowing the delete operation.
    /// Tenant scoping is enforced by <see cref="ExistsTransactionByIdQuery"/> and its handler,
    /// keeping this validator free of infrastructure concerns.
    /// Returns a 404 error code if the transaction is not found.
    /// </summary>
    public class DeleteTransactionCommandValidator : ICommandValidator<DeleteTransactionCommand>
    {
        private readonly IMediator _mediator;

        public DeleteTransactionCommandValidator(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Validates the delete command by dispatching <see cref="ExistsTransactionByIdQuery"/>
        /// through the mediator. The query handler enforces tenant isolation, so this validator
        /// stays free of infrastructure dependencies.
        /// </summary>
        /// <param name="command">The delete command to validate.</param>
        /// <returns>Validation result with a 404 error code if the transaction is not found.</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteTransactionCommand command)
        {
            var validationResult = new ValidationResult();

            if (!await _mediator.ProcessQueryAsync(new ExistsTransactionByIdQuery(command.TransactionId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.TransactionId),
                    "Transaction with ID {0} not found", command.TransactionId, Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return validationResult;
        }
    }
}

