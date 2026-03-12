using System.Threading;
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
        /// <summary>
        /// Execute the given query returning the result
        /// </summary>
        /// <param name="query">Query to be executed</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation. Defaults to CancellationToken.None if not provided.</param>
        /// <returns>Query result of type TResult</returns>
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}
