using System.Threading.Tasks;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Validation.Decorator
{
    public interface ICommandValidator<TCommand> where TCommand : ICommand
    {
        Task<IValidationResult> ValidateAsync(TCommand command);
    }
}
