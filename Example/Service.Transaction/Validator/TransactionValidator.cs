using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using System.Threading.Tasks;

namespace Service.Transaction.Validator
{
    public class TransactionValidator : IValidator<Data.Entity.Transaction>
    {
        public async Task<IValidationResult> ValidateAsync(Data.Entity.Transaction subject)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(subject.Description))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Description), "{0} is mandatory"));
            }

            if (subject.Credit == 0 && subject.Debit == 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(Data.Entity.Transaction), "{0} must have either a Debit or Credit value"));
            }

            if (subject.CategoryId == 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.CategoryId), "{0} is mandatory"));
            }
            
            return await Task.FromResult(validationResult);
        }
    }
}
