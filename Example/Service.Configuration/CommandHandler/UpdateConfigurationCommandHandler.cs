using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Configuration;
using Data.Entity;
using Minded.Framework.CQRS.Command;
using Serilog.Core;
using Service.Configuration.Command;

namespace Service.Configuration.CommandHandler
{
    /// <summary>
    /// Handler for updating configuration values.
    /// Updates the RuntimeConfigurationStore and applies special handling for Serilog level changes.
    /// The validator ensures the key exists and value is valid before this handler is called.
    /// </summary>
    public class UpdateConfigurationCommandHandler : ICommandHandler<UpdateConfigurationCommand, ConfigurationEntry>
    {
        private readonly RuntimeConfigurationStore _configStore;
        private readonly ConfigurationMetadataProvider _metadataProvider;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;

        public UpdateConfigurationCommandHandler(
            RuntimeConfigurationStore configStore,
            ConfigurationMetadataProvider metadataProvider,
            LoggingLevelSwitch loggingLevelSwitch)
        {
            _configStore = configStore;
            _metadataProvider = metadataProvider;
            _loggingLevelSwitch = loggingLevelSwitch;
        }

        public async Task<ICommandResponse<ConfigurationEntry>> HandleAsync(
            UpdateConfigurationCommand command, 
            CancellationToken cancellationToken = default)
        {
            var entry = _metadataProvider.GetMetadataByKey(command.Key);
            
            // Convert value to correct type (validator already checked this is valid)
            var convertedValue = ConvertValue(command.Request.Value, entry.Type);
            
            // Update the configuration store
            _configStore.SetValue(entry.Key, convertedValue);
            
            // Apply special handling for Serilog logging level
            if (entry.Key == "System.MinimumLogLevel")
            {
                ApplySerilogLevelChange(convertedValue.ToString());
            }
            
            entry.Value = convertedValue;

            return await Task.FromResult(new CommandResponse<ConfigurationEntry>(entry));
        }

        private object ConvertValue(object value, string type)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return type.ToLowerInvariant() switch
            {
                "bool" => Convert.ToBoolean(value),
                "int" => Convert.ToInt32(value),
                "string" => value.ToString(),
                _ => throw new NotSupportedException($"Type '{type}' is not supported.")
            };
        }

        private void ApplySerilogLevelChange(string level)
        {
            var serilogLevel = level?.ToLowerInvariant() switch
            {
                "verbose" => Serilog.Events.LogEventLevel.Verbose,
                "debug" => Serilog.Events.LogEventLevel.Debug,
                "information" => Serilog.Events.LogEventLevel.Information,
                "warning" => Serilog.Events.LogEventLevel.Warning,
                "error" => Serilog.Events.LogEventLevel.Error,
                "fatal" => Serilog.Events.LogEventLevel.Fatal,
                _ => Serilog.Events.LogEventLevel.Information
            };

            _loggingLevelSwitch.MinimumLevel = serilogLevel;
        }
    }
}

