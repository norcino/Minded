using System;
using Data.Entity;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Configuration.Command
{
    /// <summary>
    /// Command to update a configuration value by key.
    /// Validates the key exists and the value is compatible with the configuration type.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// </summary>
    [ValidateCommand]
    public class UpdateConfigurationCommand : ICommand<ConfigurationEntry>, ILoggable
    {
        public string Key { get; }
        public UpdateConfigurationRequest Request { get; }

        public UpdateConfigurationCommand(string key, UpdateConfigurationRequest request, Guid? traceId = null)
        {
            Key = key;
            Request = request;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        
        public string LoggingTemplate => "ConfigurationKey: {Key}, NewValue: {Value}";
        
        public string[] LoggingProperties => new[] { nameof(Key), "Request.Value" };
    }
}

