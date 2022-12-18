using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    internal interface ILoggableCommand : ICommand, ILoggable
    {
    }
}
