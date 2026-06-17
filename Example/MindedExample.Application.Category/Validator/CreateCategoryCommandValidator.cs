using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Minded.Extensions.Authorization;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using MindedExample.Application.Category.Command;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.Category.Validator
{
    /// <summary>
    /// Validator for <see cref="CreateCategoryCommand"/>.
    /// Enforces that the category payload is structurally valid, that the assigned user
    /// exists in the current tenant, and that the caller has permission to create root-level
    /// categories when <c>ParentId</c> is null.
    /// </summary>
    public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
    {
        private readonly IValidator<MindedExample.Domain.Category> _categoryValidator;
        private readonly IMediator _mediator;
        private readonly IAuthorizationContextAccessor _authContextAccessor;

        public CreateCategoryCommandValidator(
            IValidator<MindedExample.Domain.Category> categoryValidator,
            IMediator mediator,
            IAuthorizationContextAccessor authContextAccessor = null)
        {
            _categoryValidator = categoryValidator;
            _mediator = mediator;
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

            if (command.Category.Id != 0)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Category.Id), "{0} should not be specified on creation", GenericErrorCodes.ValidationFailed, Severity.Error));
            }

            // Verify the user assigned to the category exists in the current tenant.
            if (command.Category.UserId != 0)
            {
                var userExists = await _mediator.ProcessQueryAsync(
                    new ExistsUserInCurrentTenantQuery(command.Category.UserId), CancellationToken.None);

                if (!userExists)
                {
                    validationResult.OutcomeEntries.Add(new OutcomeEntry(
                        nameof(command.Category.UserId),
                        "User with ID {0} does not exist in the current tenant",
                        command.Category.UserId,
                        Severity.Error,
                        GenericErrorCodes.ValidationFailed));
                    return validationResult;
                }
            }

            // Check CanCreateRootCategory permission for root-level categories.
            if (command.Category.ParentId == null && _authContextAccessor?.Current != null)
            {
                var permissions = _authContextAccessor.Current.Permissions;
                if (permissions != null && !permissions.Any(p => string.Equals(p, Domain.Permissions.CanCreateRootCategory, System.StringComparison.OrdinalIgnoreCase)))
                {
                    validationResult.OutcomeEntries.Add(new OutcomeEntry("ParentId", "You do not have permission to create root-level categories", GenericErrorCodes.ValidationFailed, Severity.Error));
                }
            }

            return (await _categoryValidator.ValidateAsync(command.Category)).Merge(validationResult);
        }
    }
}
