using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Interface of a command which can be supports ILoggable trait
    /// </summary>
    public interface ILoggableCommand : ICommand, ILoggable
    {
    }
}
