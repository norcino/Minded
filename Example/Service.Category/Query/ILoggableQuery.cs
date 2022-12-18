using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace Service.Category.Command
{
    internal interface ILoggableQuery<T> : IQuery<T>, ILoggable
    {
    }
}
