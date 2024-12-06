using System.Threading.Tasks;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// Generic interface for query handlers
    /// </summary>
    /// <typeparam name="TQuery">Query type</typeparam>
    /// <typeparam name="TResult">Query result type</typeparam>
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(TQuery query);
    }
}
