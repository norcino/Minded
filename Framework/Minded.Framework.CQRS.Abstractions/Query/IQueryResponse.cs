using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// Define a structured query response wrapping the generic type Result.
    /// </summary>
    /// <typeparam name="TResult">query result</typeparam>
    public interface IQueryResponse<out TResult> : IMessageResponse
    {
        TResult Result { get; }
    }
}
