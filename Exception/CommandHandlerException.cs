using Minded.Common;

namespace Minded.Exception
{
    public class CommandHandlerException<TCommand> : System.Exception where TCommand : ICommand
    {
        public TCommand Command { get; private set; }

        public CommandHandlerException(string message, System.Exception innerException, TCommand command)
            : base(message, innerException)
        {
            Command = command;
        }
    }
}