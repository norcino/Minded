using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// This interface represent a base query interface returning the type TResult
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IQuery<TResult> : IMessage
    {
    }
}
