using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Service.Category.Command;

namespace Service.Category.Validator
{
    /// <summary>
    /// Validator for DeleteCategoryCommand.
    /// Ensures the category exists before allowing the delete operation.
    /// Returns a 404 error code if the category is not found.
    /// </summary>
    public class DeleteCategoryCommandValidator : ICommandValidator<DeleteCategoryCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteCategoryCommandValidator(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates the delete command.
        /// Checks if the category exists in the database.
        /// </summary>
        /// <param name="command">The delete command to validate</param>
        /// <returns>Validation result with 404 error code if category not found</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteCategoryCommand command)
        {
            var validationResult = new ValidationResult();

            // Check if the category exists
            var exists = await _context.Categories.AnyAsync(c => c.Id == command.CategoryId);
            if (!exists)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.CategoryId), 
                    "Category with ID {0} not found",  command.CategoryId, Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return validationResult;
        }
    }
}

