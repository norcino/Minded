using System.Threading.Tasks;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Validation.Decorator
{
    /// <summary>
    /// Defines a validator for a specific query type, used by the validation decorator
    /// to validate queries before they are processed by the handler.
    /// Implement this interface and register it with the DI container to apply validation
    /// to a given query.
    /// </summary>
    /// <typeparam name="TQuery">The query type this validator is responsible for.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    public interface IQueryValidator<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Asynchronously validates the specified query.
        /// </summary>
        /// <param name="query">The query instance to validate.</param>
        /// <returns>
        /// A <see cref="IValidationResult"/> indicating whether validation passed and
        /// containing any <see cref="Minded.Framework.CQRS.Abstractions.IOutcomeEntry"/> details.
        /// </returns>
        Task<IValidationResult> ValidateAsync(TQuery query);
    }
}
