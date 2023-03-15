namespace Minded.Framework.CQRS.Query.Trait
{
    public interface ICanCount
    {
        /// <summary>
        /// When true is the query only returns the count but no data
        /// </summary>
        bool CountOnly { get; set; }

        /// <summary>
        /// Describes if the query should also return the the number of rows matching the query criteria
        /// </summary>
        bool Count { get; set; }

        /// <summary>
        /// This property will contain the count value matching the query criteria
        /// </summary>
        int CountValue { get; set; }
    }
}
