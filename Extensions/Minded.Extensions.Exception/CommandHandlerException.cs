using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Exception
{
    public class CommandHandlerException<TCommand> : System.Exception where TCommand : ICommand
    {
        public TCommand Command { get; private set; }

        public CommandHandlerException(TCommand command, string message, System.Exception innerException = null) : base(message, innerException)
        {
            Command = command;
        }
    }
}
