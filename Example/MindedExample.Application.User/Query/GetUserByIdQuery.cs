using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query to retrieve a single user by ID within the current tenant.
    /// Returns <c>null</c> if the user is not found, causing the REST mediator to return 404.
    /// [ValidateQuery] cannot be used here: query validation requires the IQuery&lt;IQueryResponse&lt;T&gt;&gt; envelope shape.
    /// </summary>
    public class GetUserByIdQuery : IQuery<MindedExample.Domain.User>, ILoggable
    {
        /// <summary>Gets the ID of the user to retrieve.</summary>
        public int UserId { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "UserId: {UserId}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(UserId)];

        /// <summary>
        /// Initializes a new <see cref="GetUserByIdQuery"/>.
        /// </summary>
        public GetUserByIdQuery(int userId, Guid? traceId = null)
        {
            UserId = userId;
            TraceId = traceId ?? TraceId;
        }
    }
}