using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;

namespace Service.Category.Validator
{
    public class CategoryValidator : IValidator<Data.Entity.Category>
    {
        public async Task<IValidationResult> ValidateAsync(Data.Entity.Category subject)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(subject.Name))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Name), "{0} is mandatory"));
            }

            return await Task.FromResult(validationResult);
        }
    }
}
