using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Application.Api.Models;
using Application.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers
{
    /// <summary>
    /// Controller for managing runtime configuration of Minded decorators.
    /// Allows viewing and updating configuration options at runtime without application restart.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationsController : ControllerBase
    {
        private readonly RuntimeConfigurationStore _configStore;

        /// <summary>
        /// Initializes a new instance of the ConfigurationsController.
        /// </summary>
        /// <param name="configStore">The runtime configuration store.</param>
        public ConfigurationsController(RuntimeConfigurationStore configStore)
        {
            _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        }

        /// <summary>
        /// Gets all configuration entries with metadata.
        /// </summary>
        /// <returns>A list of all configuration entries.</returns>
        [HttpGet]
        public ActionResult<IEnumerable<ConfigurationEntry>> GetAll()
        {
            var configurations = _configStore.GetAllConfigurations();
            var entries = GetConfigurationMetadata();

            // Update current values from store
            foreach (var entry in entries)
            {
                if (configurations.TryGetValue(entry.Key, out var value))
                {
                    entry.Value = value;
                }
            }

            return Ok(entries);
        }

        /// <summary>
        /// Gets a specific configuration entry by key.
        /// </summary>
        /// <param name="key">The configuration key (e.g., "Logging.Enabled").</param>
        /// <returns>The configuration entry if found, otherwise NotFound.</returns>
        [HttpGet("{key}")]
        public ActionResult<ConfigurationEntry> GetByKey(string key)
        {
            var entries = GetConfigurationMetadata();
            var entry = entries.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                return NotFound(new { message = $"Configuration key '{key}' not found." });
            }

            var configurations = _configStore.GetAllConfigurations();

            if (configurations.TryGetValue(entry.Key, out var value))
            {
                entry.Value = value;
            }

            return Ok(entry);
        }

        /// <summary>
        /// DEBUG endpoint: Gets the raw value directly from the store without metadata.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The raw value from the store.</returns>
        [HttpGet("debug/{key}")]
        public ActionResult<object> GetDebug(string key)
        {
            var allConfigs = _configStore.GetAllConfigurations();
            var value = _configStore.GetValue<object>(key);

            return Ok(new
            {
                Key = key,
                Value = value,
                ValueType = value?.GetType().Name,
                ExistsInStore = allConfigs.ContainsKey(key),
                AllKeys = allConfigs.Keys.ToList(),
                StoreInstanceId = _configStore.InstanceId
            });
        }

        /// <summary>
        /// Updates a configuration value by key.
        /// </summary>
        /// <param name="key">The configuration key (e.g., "Logging.Enabled").</param>
        /// <param name="request">The update request containing the new value.</param>
        /// <returns>The updated configuration entry.</returns>
        [HttpPut("{key}")]
        public ActionResult<ConfigurationEntry> Update(string key, [FromBody] UpdateConfigurationRequest request)
        {
            var entries = GetConfigurationMetadata();
            var entry = entries.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                return NotFound(new { message = $"Configuration key '{key}' not found." });
            }

            // Validate and convert value based on type
            object convertedValue;
            try
            {
                convertedValue = ConvertValue(request.Value, entry.Type);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Invalid value for type '{entry.Type}': {ex.Message}" });
            }

            // Update the configuration store
            _configStore.SetValue(entry.Key, convertedValue);

            entry.Value = convertedValue;

            // Apply special handling for Serilog logging level
            if (entry.Key == "System.MinimumLogLevel")
            {
                ApplySerilogLevelChange(convertedValue.ToString());
            }

            return Ok(entry);
        }

        /// <summary>
        /// Applies Serilog logging level changes at runtime.
        /// </summary>
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

            Startup.LoggingLevelSwitch.MinimumLevel = serilogLevel;
        }

        /// <summary>
        /// Converts a value to the specified type.
        /// </summary>
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

        /// <summary>
        /// Gets metadata for all configuration options.
        /// This defines the schema and descriptions for all available configuration options.
        /// </summary>
        private List<ConfigurationEntry> GetConfigurationMetadata()
        {
            return new List<ConfigurationEntry>
            {
                // System Options (1 option)
                new ConfigurationEntry
                {
                    Key = "System.MinimumLogLevel",
                    Category = "System",
                    Name = "MinimumLogLevel",
                    Type = "string",
                    DefaultValue = "Information",
                    Description = "Application-wide minimum logging level (Serilog). Controls verbosity of all logs. Options: Verbose, Debug, Information, Warning, Error, Fatal."
                },

                // Logging Options (4 options)
                new ConfigurationEntry
                {
                    Key = "Logging.Enabled",
                    Category = "Logging",
                    Name = "Enabled",
                    Type = "bool",
                    DefaultValue = false,
                    Description = "Enables or disables logging decorator. When false, no logging is performed."
                },
                new ConfigurationEntry
                {
                    Key = "Logging.LogMessageTemplateData",
                    Category = "Logging",
                    Name = "LogMessageTemplateData",
                    Type = "bool",
                    DefaultValue = false,
                    Description = "When true, logs the message template data (command/query properties) in structured format."
                },
                new ConfigurationEntry
                {
                    Key = "Logging.LogOutcomeEntries",
                    Category = "Logging",
                    Name = "LogOutcomeEntries",
                    Type = "bool",
                    DefaultValue = false,
                    Description = "When true, logs individual outcome entries (errors, warnings, info messages)."
                },
                new ConfigurationEntry
                {
                    Key = "Logging.MinimumOutcomeSeverityLevel",
                    Category = "Logging",
                    Name = "MinimumOutcomeSeverityLevel",
                    Type = "string",
                    DefaultValue = "Info",
                    Description = "Minimum severity level for logging outcome entries. Options: Info, Warning, Error."
                },

                // Exception Options (1 option)
                new ConfigurationEntry
                {
                    Key = "Exception.Serialize",
                    Category = "Exception",
                    Name = "Serialize",
                    Type = "bool",
                    DefaultValue = true,
                    Description = "When true, serializes exception details into the command response outcome."
                },

                // Retry Options (7 options)
                new ConfigurationEntry
                {
                    Key = "Retry.ApplyToAllQueries",
                    Category = "Retry",
                    Name = "ApplyToAllQueries",
                    Type = "bool",
                    DefaultValue = false,
                    Description = "When true, applies retry logic to ALL queries even without [RetryQuery] attribute."
                },
                new ConfigurationEntry
                {
                    Key = "Retry.DefaultRetryCount",
                    Category = "Retry",
                    Name = "DefaultRetryCount",
                    Type = "int",
                    DefaultValue = 3,
                    Description = "Default number of retry attempts when not specified in attribute."
                },
                new ConfigurationEntry
                {
                    Key = "Retry.DefaultDelay1",
                    Category = "Retry",
                    Name = "DefaultDelay1",
                    Type = "int",
                    DefaultValue = 0,
                    Description = "Default delay in milliseconds before first retry."
                },
                new ConfigurationEntry
                {
                    Key = "Retry.DefaultDelay2",
                    Category = "Retry",
                    Name = "DefaultDelay2",
                    Type = "int",
                    DefaultValue = 0,
                    Description = "Default delay in milliseconds before second retry."
                },
                new ConfigurationEntry
                {
                    Key = "Retry.DefaultDelay3",
                    Category = "Retry",
                    Name = "DefaultDelay3",
                    Type = "int",
                    DefaultValue = 0,
                    Description = "Default delay in milliseconds before third retry."
                },
                new ConfigurationEntry
                {
                    Key = "Retry.DefaultDelay4",
                    Category = "Retry",
                    Name = "DefaultDelay4",
                    Type = "int",
                    DefaultValue = 0,
                    Description = "Default delay in milliseconds before fourth retry."
                },
                new ConfigurationEntry
                {
                    Key = "Retry.DefaultDelay5",
                    Category = "Retry",
                    Name = "DefaultDelay5",
                    Type = "int",
                    DefaultValue = 0,
                    Description = "Default delay in milliseconds before fifth retry."
                },

                // DataProtection Options (1 option)
                new ConfigurationEntry
                {
                    Key = "DataProtection.ShowSensitiveData",
                    Category = "DataProtection",
                    Name = "ShowSensitiveData",
                    Type = "bool",
                    DefaultValue = false,
                    Description = "When true, shows sensitive data (e.g., passwords, credit cards) without sanitization. Use only in development."
                },

                // Transaction Options (5 options)
                new ConfigurationEntry
                {
                    Key = "Transaction.DefaultTransactionScopeOption",
                    Category = "Transaction",
                    Name = "DefaultTransactionScopeOption",
                    Type = "string",
                    DefaultValue = "Required",
                    Description = "Default transaction scope option. Options: Required, RequiresNew, Suppress."
                },
                new ConfigurationEntry
                {
                    Key = "Transaction.DefaultIsolationLevel",
                    Category = "Transaction",
                    Name = "DefaultIsolationLevel",
                    Type = "string",
                    DefaultValue = "ReadCommitted",
                    Description = "Default isolation level. Options: ReadCommitted, ReadUncommitted, RepeatableRead, Serializable."
                },
                new ConfigurationEntry
                {
                    Key = "Transaction.DefaultTimeoutSeconds",
                    Category = "Transaction",
                    Name = "DefaultTimeoutSeconds",
                    Type = "int",
                    DefaultValue = 60,
                    Description = "Default transaction timeout in seconds. Transactions exceeding this will be rolled back."
                },
                new ConfigurationEntry
                {
                    Key = "Transaction.RollbackOnUnsuccessfulResponse",
                    Category = "Transaction",
                    Name = "RollbackOnUnsuccessfulResponse",
                    Type = "bool",
                    DefaultValue = true,
                    Description = "When true, rolls back transaction if command response is unsuccessful. When false, only exceptions cause rollback."
                },
                new ConfigurationEntry
                {
                    Key = "Transaction.EnableLogging",
                    Category = "Transaction",
                    Name = "EnableLogging",
                    Type = "bool",
                    DefaultValue = true,
                    Description = "When true, logs transaction start/complete/rollback events at Information level."
                }
            };
        }
    }

    /// <summary>
    /// Request model for updating a configuration value.
    /// </summary>
    public class UpdateConfigurationRequest
    {
        /// <summary>
        /// Gets or sets the new value for the configuration.
        /// </summary>
        public object Value { get; set; }
    }
}

