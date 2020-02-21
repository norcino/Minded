using System.Threading.Tasks;
using Minded.Common;
using Minded.Validation;

namespace Minded.Decorator.Validation
{
    public interface ICommandValidator<TCommand> where TCommand : ICommand
    {
        Task<IValidationResult> ValidateAsync(TCommand command);
    }
}