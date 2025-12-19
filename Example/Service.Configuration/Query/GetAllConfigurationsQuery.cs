using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using Data.Entity;

namespace Service.Configuration.Query
{
    /// <summary>
    /// Query to retrieve all configuration entries with current values and metadata.
    /// Returns a list of all available configuration options across all categories.
    /// </summary>
    public class GetAllConfigurationsQuery : IQuery<IQueryResponse<IEnumerable<ConfigurationEntry>>>, ILoggable
    {
        public GetAllConfigurationsQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        
        public string LoggingTemplate => "Retrieving all configuration entries";
        
        public string[] LoggingProperties => Array.Empty<string>();
    }
}

