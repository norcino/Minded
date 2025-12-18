using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Service.User.Command;

namespace Service.User.Validator
{
    /// <summary>
    /// Validator for DeleteUserCommand.
    /// Ensures the user exists before allowing the delete operation.
    /// Returns a 404 error code if the user is not found.
    /// </summary>
    public class DeleteUserCommandValidator : ICommandValidator<DeleteUserCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteUserCommandValidator(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates the delete command.
        /// Checks if the user exists in the database.
        /// </summary>
        /// <param name="command">The delete command to validate</param>
        /// <returns>Validation result with 404 error code if user not found</returns>
        public async Task<IValidationResult> ValidateAsync(DeleteUserCommand command)
        {
            var validationResult = new ValidationResult();

            // Check if the user exists
            var exists = await _context.Users.AnyAsync(u => u.Id == command.UserId);
            if (!exists)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(
                    nameof(command.UserId), 
                    "User with ID {0} not found", command.UserId, Severity.Error, GenericErrorCodes.SubjectNotFound));
            }

            return validationResult;
        }
    }
}

