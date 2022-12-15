using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Exception
{
    public class CommandHandlerException<TCommand> : System.Exception where TCommand : ICommand
    {
        public TCommand Command { get; private set; }

        public CommandHandlerException(string message, System.Exception innerException, TCommand command, string errorCode = "0")
    : base(message, innerException)
        {
            Command = command;
        }
    }
}
