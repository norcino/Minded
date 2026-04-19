using System.Threading.Tasks;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Validation.Decorator
{
    /// <summary>
    /// Defines a validator for a specific command type, used by the validation decorator
    /// to validate commands before they are processed by the handler.
    /// Implement this interface and register it with the DI container to apply validation
    /// to a given command.
    /// </summary>
    /// <typeparam name="TCommand">The command type this validator is responsible for.</typeparam>
    public interface ICommandValidator<TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Asynchronously validates the specified command.
        /// </summary>
        /// <param name="command">The command instance to validate.</param>
        /// <returns>
        /// A <see cref="IValidationResult"/> indicating whether validation passed and
        /// containing any <see cref="Minded.Framework.CQRS.Abstractions.IOutcomeEntry"/> details.
        /// </returns>
        Task<IValidationResult> ValidateAsync(TCommand command);
    }
}
