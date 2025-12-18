using System.Threading.Tasks;
using Minded.Extensions.Exception;
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
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Name), "{0} is mandatory", subject.Name, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            if (subject.Name?.Length < 4)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Name), "It is recoomended to use {0} with more than 3 characters", subject.Name, Severity.Warning));
            }

            if (subject.ParentId == subject.Id)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.ParentId), "{0} does not need to be set for root categories", subject.Name, Severity.Info));
            }

            return await Task.FromResult(validationResult);
        }
    }
}
