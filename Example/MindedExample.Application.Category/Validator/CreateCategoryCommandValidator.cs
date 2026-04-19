using System.Linq;
using System.Threading.Tasks;
using Minded.Extensions.Authorization;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Category.Command;

namespace MindedExample.Application.Category.Validator
{
    public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
    {
        private readonly IValidator<MindedExample.Domain.Category> _categoryValidator;
        private readonly IAuthorizationContextAccessor _authContextAccessor;

        public CreateCategoryCommandValidator(
            IValidator<MindedExample.Domain.Category> categoryValidator,
            IAuthorizationContextAccessor authContextAccessor = null)
        {
            _categoryValidator = categoryValidator;
            _authContextAccessor = authContextAccessor;
        }

        public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.Category == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Category), "{0} is mandatory", GenericErrorCodes.ValidationFailed, Severity.Error));

                return validationResult;
            }

            Task<IValidationResult> result = _categoryValidator.ValidateAsync(command.Category);

            if (command.Category.Id != 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Category.Id), "{0} should not be specified on creation", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            // Check CanCreateRootCategory permission for root-level categories
            if (command.Category.ParentId == null && _authContextAccessor?.Current != null)
            {
                var permissions = _authContextAccessor.Current.Permissions;
                if (permissions != null && !permissions.Any(p => string.Equals(p, Domain.Permissions.CanCreateRootCategory, System.StringComparison.OrdinalIgnoreCase)))
                {
                    validationResult.OutcomeEntries.Add(new OutcomeEntry("ParentId", "You do not have permission to create root-level categories", GenericErrorCodes.ValidationFailed, Severity.Error));
                }
            }

            return (await result).Merge(validationResult);
        }
    }
}
