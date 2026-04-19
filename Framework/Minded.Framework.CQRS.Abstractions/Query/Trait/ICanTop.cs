namespace Minded.Framework.CQRS.Query.Trait
{
    /// <summary>
    /// Trait that enables a query to limit the number of results returned.
    /// Commonly used together with <see cref="ICanSkip"/> to implement server-side pagination.
    /// </summary>
    public interface ICanTop
    {
        /// <summary>
        /// Maximum number of results to return.
        /// When <c>null</c> no limit is applied (subject to any default enforced by the handler).
        /// </summary>
        int? Top { get; set; }
    }
}
