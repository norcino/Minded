using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using System.Threading.Tasks;

namespace Service.Category.Validator
{
    public class CategoryValidator : IValidator<Data.Entity.Category>
    {
        public async Task<IValidationResult> ValidateAsync(Data.Entity.Category subject)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(subject.Name))
            {
                validationResult.ValidationEntries.Add(new OutcomeEntry(nameof(subject.Name), "{0} is mandatory"));
            }

            return await Task.FromResult(validationResult);
        }
    }
}
