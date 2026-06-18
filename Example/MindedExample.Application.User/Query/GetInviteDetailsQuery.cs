using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query to retrieve public information about a tenant invite by its token or short code.
    /// Returns <c>null</c> when the invite is not found, expired, or already used,
    /// causing the REST mediator to return <c>404 Not Found</c>.
    /// </summary>
    public class GetInviteDetailsQuery : IQuery<InviteDetailsResult>, ILoggable
    {
        /// <summary>Gets the invite token (UUID-format URL link) or short code.</summary>
        public string TokenOrCode { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "GetInviteDetails";

        /// <inheritdoc />
        public string[] LoggingProperties => [];

        /// <summary>
        /// Initializes a new <see cref="GetInviteDetailsQuery"/>.
        /// </summary>
        public GetInviteDetailsQuery(string tokenOrCode, Guid? traceId = null)
        {
            TokenOrCode = tokenOrCode;
            TraceId = traceId ?? TraceId;
        }
    }
}
