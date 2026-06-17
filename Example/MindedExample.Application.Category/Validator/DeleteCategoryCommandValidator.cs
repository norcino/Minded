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
    /// Validator for DeleteCategoryCommand.
    /// Ensures the category exists before allowing the delete operation.
    /// Tenant-scoping is handled by <see cref="ExistsCategoryInCurrentTenantQuery"/> and its handler,
    /// keeping the validator free of infrastructure concerns.
    /// Returns a 404 error code if the category is not found.
    /// </summary>
    public class DeleteCategoryCommandValidator : ICommandValidator<DeleteCategoryCommand>
    {
        private readonly IMediator _mediator;

        public DeleteCategoryCommandValidator(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Validates the delete command by dispatching <see cref="ExistsCategoryInCurrentTenantQuery"/>
        /// through the mediator. The query handler enforces tenant isolation, so this validator
        /// stays free of infrastructure dependencies.
        /// </summary>
        /// <param name="command">The delete command to validate.</param>
        /// <returns>Validation result with a 404 error code if the category is not found.</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteCategoryCommand command)
        {
            var validationResult = new ValidationResult();

            if (!await _mediator.ProcessQueryAsync(new ExistsCategoryInCurrentTenantQuery(command.CategoryId)))
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.CategoryId),
                    "Category with ID {0} not found", command.CategoryId, Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return validationResult;
        }
    }
}

