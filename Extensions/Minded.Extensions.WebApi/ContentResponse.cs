namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Controls what content is included in the HTTP response body when a REST rule matches.
    /// </summary>
    public enum ContentResponse : int
    {
        /// <summary>No body is returned (e.g. HTTP 204 No Content).</summary>
        None = 0,

        /// <summary>
        /// Returns only the strongly-typed <c>Result</c> property of an
        /// <see cref="Minded.Framework.CQRS.Command.ICommandResponse{TResult}"/>.
        /// Falls back to <see cref="Full"/> for commands/queries that do not carry a typed result.
        /// </summary>
        Result = 1,

        /// <summary>Returns the complete command or query response object in the body.</summary>
        Full = 2
    }
}
