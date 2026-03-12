using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Query;
using System;

namespace Service.User.Query
{
    /// <summary>
    /// Query to retrieve a single user by ID.
    /// Uses MemoryCache decorator to cache results for 60 seconds.
    /// User's sensitive data (name, surname, email) will be protected in logs by the DataProtectionLoggingSanitizer.
    /// </summary>
    [MemoryCache(60)]
    public class GetUserByIdQuery : IQuery<Data.Entity.User>, ILoggable
    {
        public int UserId { get; }

        public GetUserByIdQuery(int id, Guid? traceId = null)
        {
            UserId = id;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "UserId: {UserId}";

        public string[] LoggingProperties => new[] { nameof(UserId) };
    }
}

