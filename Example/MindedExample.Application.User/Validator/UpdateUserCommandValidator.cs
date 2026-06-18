using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Validator
{
    /// <summary>
    /// Validator for UpdateUserCommand.
    /// Ensures the user exists before allowing the update operation.
    /// Returns a 404 error code if the user is not found.
    /// </summary>
    public class UpdateUserCommandValidator : ICommandValidator<UpdateUserCommand>
    {
        private readonly IValidator<MindedExample.Domain.User> _userValidator;
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public UpdateUserCommandValidator(
            IValidator<MindedExample.Domain.User> userValidator,
            IMindedExampleContext context,
            ICurrentUserAccessor currentUserAccessor)
        {
            _userValidator = userValidator;
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Validates the update command.
        /// Checks if the user exists and validates the user entity.
        /// </summary>
        /// <param name="command">The update command to validate</param>
        /// <returns>Validation result with 404 error code if user not found</returns>
        public async Task<IValidationResult> ValidateAsync(UpdateUserCommand command)
        {
            var validationResult = new ValidationResult();

            if (command.User == null)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(command.User), "{0} is mandatory", null, Severity.Error, GenericErrorCodes.ValidationFailed));
                return validationResult;
            }

            if(command.UserId != command.User.Id)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), "User ID in command does not match User entity ID", command.UserId, Severity.Error, GenericErrorCodes.ValidationFailed));
                return validationResult;
            }

            // Check if the user exists in the caller's tenant (tenant-scoped so foreign-tenant
            // ids are indistinguishable from nonexistent ids)
            var tenantId = _currentUserAccessor.TenantId;
            var exists = tenantId.HasValue && await _context.Users
                .AnyAsync(u => u.Id == command.UserId && u.TenantId == tenantId.Value);
            if (!exists)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), "User with ID {0} not found", command.UserId, Severity.Error, GenericErrorCodes.SubjectNotFound));
                return validationResult;
            }

            // Validate the user entity
            IValidationResult userValidationResult = await _userValidator.ValidateAsync(command.User);
            return userValidationResult.Merge(validationResult);
        }
    }
}

