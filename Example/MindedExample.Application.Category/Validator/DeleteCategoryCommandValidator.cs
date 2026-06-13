using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Category.Command;

namespace MindedExample.Application.Category.Validator
{
    /// <summary>
    /// Validator for DeleteCategoryCommand.
    /// Ensures the category exists before allowing the delete operation.
    /// Returns a 404 error code if the category is not found.
    /// </summary>
    public class DeleteCategoryCommandValidator : ICommandValidator<DeleteCategoryCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteCategoryCommandValidator(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Validates the delete command.
        /// Checks if the category exists within the caller's tenant. The check must be
        /// tenant-scoped: an unscoped check would answer differently for foreign-tenant ids
        /// and nonexistent ids, leaking which ids exist in other tenants.
        /// </summary>
        /// <param name="command">The delete command to validate</param>
        /// <returns>Validation result with 404 error code if category not found</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteCategoryCommand command)
        {
            var validationResult = new ValidationResult();

            // Check if the category exists in the caller's tenant
            var tenantId = _currentUserAccessor.TenantId;
            var exists = tenantId.HasValue && await _context.Categories
                .AnyAsync(c => c.Id == command.CategoryId && c.User.TenantId == tenantId.Value);
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

