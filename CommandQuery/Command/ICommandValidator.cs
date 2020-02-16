using System.Threading.Tasks;
using Minded.Validation;

namespace Minded.Common
{
    public interface ICommandValidator<TCommand> where TCommand : ICommand
    {
        Task<IValidationResult> ValidateAsync(TCommand command);
    }
}