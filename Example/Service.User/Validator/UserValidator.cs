using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;

namespace Service.User.Validator
{
    /// <summary>
    /// Validator for User entity.
    /// Validates that required fields (Name, Surname, Email) are provided.
    /// </summary>
    public class UserValidator : IValidator<Data.Entity.User>
    {
        public async Task<IValidationResult> ValidateAsync(Data.Entity.User subject)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(subject.Name))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Name), "{0} is mandatory", subject.Name, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            if (string.IsNullOrWhiteSpace(subject.Surname))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Surname), "{0} is mandatory", subject.Surname, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            if (string.IsNullOrWhiteSpace(subject.Email))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Email), "{0} is mandatory", subject.Email, Severity.Error, GenericErrorCodes.ValidationFailed));
            }
            else if (!IsValidEmail(subject.Email))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Email), "{0} is not a valid email address", subject.Email, Severity.Error, GenericErrorCodes.ValidationFailed));
            }

            return await Task.FromResult(validationResult);
        }

        /// <summary>
        /// Simple email validation.
        /// Checks if the email contains @ and . characters.
        /// </summary>
        private bool IsValidEmail(string email)
        {
            return email.Contains("@") && email.Contains(".");
        }
    }
}

