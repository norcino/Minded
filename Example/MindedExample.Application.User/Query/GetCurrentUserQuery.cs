using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query to retrieve the currently authenticated user's profile along with a tenant summary.
    /// Returns <c>null</c> when no authenticated user is found, causing the REST mediator to
    /// return <c>404 Not Found</c> (via <see cref="Minded.Extensions.WebApi.RestOperation.GetSingle"/>).
    /// </summary>
    public class GetCurrentUserQuery : IQuery<AuthResult>, ILoggable
    {
        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "GetCurrentUser";

        /// <inheritdoc />
        public string[] LoggingProperties => [];

        /// <summary>
        /// Initializes a new <see cref="GetCurrentUserQuery"/>.
        /// </summary>
        public GetCurrentUserQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }
    }
}
