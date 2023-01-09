using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Exception
{
    public class CommandHandlerException<TCommand> : System.Exception where TCommand : ICommand
    {
        public TCommand Command { get; private set; }
        public string ErrorCode { get; set; } = GenericErrorCodes.Unknown;

        public CommandHandlerException(TCommand command, string message, string errorCode, System.Exception innerException = null) : base(message, innerException)
        {
            Command = command;
            ErrorCode = errorCode;
        }

        public CommandHandlerException(TCommand command, string message, System.Exception innerException = null) : base(message, innerException)
        {
            Command = command;
        }
    }
}
