using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Service.Transaction.Command;

namespace Service.Transaction.Validator
{
    public class CreateTransactionCommandValidator : ICommandValidator<CreateTransactionCommand>
    {
        private readonly IValidator<Data.Entity.Transaction> _categoryValidator;

        public CreateTransactionCommandValidator(IValidator<Data.Entity.Transaction> categoryValidator)
        {
            _categoryValidator = categoryValidator;
        }

        public async Task<IValidationResult> ValidateAsync(CreateTransactionCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.Transaction == null)
            {
                validationResult.ValidationEntries.Add(new OutcomeEntry(nameof(command.Transaction), "{0} is mandatory"));

                return validationResult;
            }

            if (command.Transaction.Id != 0)
            {
                validationResult.ValidationEntries.Add(new OutcomeEntry(nameof(command.Transaction.Id), "{0} should not be specified on creation"));
            }

            var result = _categoryValidator.ValidateAsync(command.Transaction);           
            return (await result).Merge(validationResult);
        }
    }
}
