using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validates a <see cref="MindedExample.Domain.User"/> entity.
    /// Ensures that the required personal fields are present and well-formed.
    /// </summary>
    public class UserValidator : IValidator<MindedExample.Domain.User>
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <inheritdoc />
        public async Task<IValidationResult> ValidateAsync(MindedExample.Domain.User subject)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(subject.Name))
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Name), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(subject.Surname))
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Surname), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

            if (string.IsNullOrWhiteSpace(subject.Email))
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Email), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));
            else if (!EmailRegex.IsMatch(subject.Email))
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(subject.Email), "{0} is not a valid email address", GenericErrorCodes.ValidationFailed, Severity.Error));

            return await Task.FromResult(validationResult);
        }
    }
}
