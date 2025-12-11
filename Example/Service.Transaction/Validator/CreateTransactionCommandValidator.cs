using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Service.Category.Query;
using Service.Transaction.Command;

namespace Service.Transaction.Validator
{
    public class CreateTransactionCommandValidator : ICommandValidator<CreateTransactionCommand>
    {
        private readonly IValidator<Data.Entity.Transaction> _categoryValidator;
        private readonly IMediator _mediator;

        public CreateTransactionCommandValidator(IValidator<Data.Entity.Transaction> categoryValidator, IMediator mediator)
        {
            _categoryValidator = categoryValidator;
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
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction.Id), "{0} should not be specified on creation", GenericErrorCodes.BadRequest.ToString(), Severity.Error));
            }

            if(command.Transaction.CategoryId == 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction.CategoryId), "{0} is mandatory", GenericErrorCodes.BadRequest.ToString(), Severity.Error));
            }

            if(!await _mediator.ProcessQueryAsync(new ExistsCategoryByIdQuery(command.Transaction.CategoryId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Transaction.CategoryId), "{0} references a non-existing category", GenericErrorCodes.BadRequest.ToString(), Severity.Error));
            }

            Task<IValidationResult> result = _categoryValidator.ValidateAsync(command.Transaction);
            return (await result).Merge(validationResult);
        }
    }
}
