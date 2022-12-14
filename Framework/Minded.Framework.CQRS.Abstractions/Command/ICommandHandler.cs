using System.Threading.Tasks;

namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Describes an handler responsible to process the specified type of command.
    /// If the command type is a base type, the handler will be invoked to process all the command type implementations.
    /// More than one handler can be created to process a single command type.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command the handler will execute</typeparam>
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Execute the given command returning the <see cref="ICommandResponse"/>
        /// </summary>
        /// <param name="command">Command to be executed</param>
        /// <returns><see cref="ICommandResponse"/> containing the command output</returns>
        Task<ICommandResponse> HandleAsync(TCommand command);
    }
}
