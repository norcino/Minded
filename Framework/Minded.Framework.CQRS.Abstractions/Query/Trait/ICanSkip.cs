namespace Minded.Framework.CQRS.Query.Trait
{
    /// <summary>
    /// Trait that enables a query to skip a specified number of results.
    /// Commonly used together with <see cref="ICanTop"/> to implement server-side pagination.
    /// </summary>
    public interface ICanSkip
    {
        /// <summary>
        /// Number of results to skip before returning data.
        /// When <c>null</c> no records are skipped.
        /// </summary>
        int? Skip { get; set; }
    }
}
