using System.Threading.Tasks;
using Minded.Common;
using Minded.Decorator.Validation;
using Minded.Validation;
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
                validationResult.ValidationEntries.Add(new ValidationEntry(nameof(command.Category), "{0} is mandatory"));

                return validationResult;
            }

            var result = _categoryValidator.ValidateAsync(command.Category);

            if (command.Category.Id != 0)
            {
                validationResult.ValidationEntries.Add(new ValidationEntry(nameof(command.Category.Id), "{0} should not be specified on creation"));
            }
            
            return (await result).Merge(validationResult);
        }
    }
}
