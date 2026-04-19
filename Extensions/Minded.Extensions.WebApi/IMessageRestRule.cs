using System.Net;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Base rule that maps a REST operation outcome to an HTTP status code and content strategy.
    /// Command-specific and query-specific rules extend this interface to add their own condition logic.
    /// </summary>
    public interface IMessageRestRule
    {
        /// <summary>
        /// The <see cref="RestOperation"/> to which this rule applies.
        /// Use <see cref="RestOperation.Any"/> to match all operations.
        /// </summary>
        RestOperation Operation { get; }

        /// <summary>
        /// HTTP status code to return when this rule's condition is satisfied.
        /// </summary>
        HttpStatusCode ResultStatusCode { get; }

        /// <summary>
        /// Determines what content, if any, is included in the HTTP response body.
        /// </summary>
        ContentResponse ContentResponse { get; }
    }
}
