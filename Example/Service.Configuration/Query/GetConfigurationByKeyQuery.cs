using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Query;
using Data.Entity;

namespace Service.Configuration.Query
{
    /// <summary>
    /// Query to retrieve a specific configuration entry by key.
    /// Returns the configuration with current value and metadata.
    /// </summary>
    [ValidateQuery]
    public class GetConfigurationByKeyQuery : IQuery<ConfigurationEntry>, ILoggable
    {
        public string Key { get; }

        public GetConfigurationByKeyQuery(string key, Guid? traceId = null)
        {
            Key = key;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        
        public string LoggingTemplate => "ConfigurationKey: {Key}";
        
        public string[] LoggingProperties => new[] { nameof(Key) };
    }
}

