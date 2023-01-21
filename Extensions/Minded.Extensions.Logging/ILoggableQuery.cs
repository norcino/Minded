using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Interface of a query which can be supports ILoggable trait
    /// </summary>
    /// <typeparam name="T">Query return type</typeparam>
    public interface ILoggableQuery<T> : IQuery<T>, ILoggable
    {
    }
}
