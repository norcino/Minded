using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using MindedExample.Application.Category.Command;
using MindedExample.Application.Category.Query;

namespace MindedExample.Application.Category.Validator
{
    /// <summary>
    /// Validator for UpdateCategoryCommand.
    /// Ensures the category exists before allowing the update operation.
    /// Returns a 404 error code if the category is not found.
    /// </summary>
    public class UpdateCategoryCommandValidator : ICommandValidator<UpdateCategoryCommand>
    {
        private readonly IValidator<MindedExample.Domain.Category> _categoryValidator;
        private readonly IMediator _mediator;

        public UpdateCategoryCommandValidator(IValidator<MindedExample.Domain.Category> categoryValidator, IMediator mediator)
        {
            _categoryValidator = categoryValidator;
            _mediator = mediator;
        }

        /// <summary>
        /// Validates the update command.
        /// Checks if the category exists and validates the category entity.
        /// </summary>
        /// <param name="command">The update command to validate</param>
        /// <returns>Validation result with 404 error code if category not found</returns>
        public async Task<IValidationResult> ValidateAsync(UpdateCategoryCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.Category == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Category), "{0} is mandatory", null, Severity.Error, GenericErrorCodes.ValidationFailed));
                return validationResult;
            }

            if(command.CategoryId != command.Category.Id)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.CategoryId), "Category ID in command does not match Category entity ID", command.CategoryId, Severity.Error, GenericErrorCodes.ValidationFailed));
                return validationResult;
            }

            if (!await _mediator.ProcessQueryAsync(new ExistsCategoryInCurrentTenantQuery(command.CategoryId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.CategoryId), "Category with ID {0} not found", command.CategoryId, Severity.Error, GenericErrorCodes.SubjectNotFound));
                return validationResult;
            }

            // Validate the category entity
            IValidationResult categoryValidationResult = await _categoryValidator.ValidateAsync(command.Category);
            return categoryValidationResult.Merge(validationResult);
        }
    }
}

