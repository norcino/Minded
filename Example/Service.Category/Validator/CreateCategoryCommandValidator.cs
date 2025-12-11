using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Service.Category.Command;

namespace Service.Category.Validator
{
    public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
    {
        private readonly IValidator<Data.Entity.Category> _categoryValidator;

        public CreateCategoryCommandValidator(IValidator<Data.Entity.Category> categoryValidator)
        {
            _categoryValidator = categoryValidator;
        }

        public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.Category == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Category), "{0} is mandatory", GenericErrorCodes.BadRequest, Severity.Error));

                return validationResult;
            }

            Task<IValidationResult> result = _categoryValidator.ValidateAsync(command.Category);

            if (command.Category.Id != 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Category.Id), "{0} should not be specified on creation", GenericErrorCodes.BadRequest, Severity.Error));
            }

            return (await result).Merge(validationResult);
        }
    }
}
