using System.Threading.Tasks;

namespace Minded.Common
{
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        Task<ICommandResponse> HandleAsync(TCommand command);
    }
}