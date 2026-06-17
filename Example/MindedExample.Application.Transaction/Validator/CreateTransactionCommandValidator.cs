using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using MindedExample.Application.Category.Query;
using MindedExample.Application.Transaction.Command;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.Transaction.Validator
{
    /// <summary>
    /// Validator for <see cref="CreateTransactionCommand"/>.
    /// Ensures the transaction data is valid, the category exists, and the user belongs to the current tenant.
    /// </summary>
    public class CreateTransactionCommandValidator : ICommandValidator<CreateTransactionCommand>
    {
        private readonly IValidator<MindedExample.Domain.Transaction> _transactionValidator;
        private readonly IMediator _mediator;

        public CreateTransactionCommandValidator(IValidator<MindedExample.Domain.Transaction> transactionValidator, IMediator mediator)
        {
            _transactionValidator = transactionValidator;
            _mediator = mediator;
        }

        public async Task<IValidationResult> ValidateAsync(CreateTransactionCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.Transaction == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction), "{0} is mandatory", Severity.Error));
                return validationResult;
            }

            if (command.Transaction.Id != 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction.Id), "{0} should not be specified on creation", GenericErrorCodes.ValidationFailed.ToString(), Severity.Error));
            }

            if (command.Transaction.CategoryId == 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction.CategoryId), "{0} is mandatory", GenericErrorCodes.ValidationFailed.ToString(), Severity.Error));
            }
            else if (!await _mediator.ProcessQueryAsync(new ExistsCategoryInCurrentTenantQuery(command.Transaction.CategoryId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction.CategoryId), "{0} references a non-existing category", GenericErrorCodes.ValidationFailed.ToString(), Severity.Error));
            }

            if (command.Transaction.UserId != 0 && !await _mediator.ProcessQueryAsync(new ExistsUserInCurrentTenantQuery(command.Transaction.UserId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.Transaction.UserId),
                    "User with ID {0} does not exist in the current tenant", command.Transaction.UserId, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            return (await _transactionValidator.ValidateAsync(command.Transaction)).Merge(validationResult);
        }
    }
}
