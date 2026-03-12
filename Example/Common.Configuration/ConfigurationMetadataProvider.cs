using System.Collections.Generic;
using System.Linq;
using Data.Entity;

namespace Common.Configuration
{
    /// <summary>
    /// Provides metadata for all available configuration options.
    /// This defines the schema, types, and descriptions for runtime configuration.
    /// </summary>
    public class ConfigurationMetadataProvider
    {
        private readonly List<ConfigurationEntry> _metadata;

        public ConfigurationMetadataProvider()
        {
            _metadata = InitializeMetadata();
        }

        /// <summary>
        /// Gets all configuration metadata entries.
        /// </summary>
        /// <returns>List of all configuration entries with metadata</returns>
        public List<ConfigurationEntry> GetAllMetadata()
        {
            // Return a copy to prevent external modification
            return _metadata.Select(m => new ConfigurationEntry
            {
                Key = m.Key,
                Category = m.Category,
                Name = m.Name,
                Type = m.Type,
                DefaultValue = m.DefaultValue,
                Description = m.Description,
                Value = m.Value
            }).ToList();
        }

        /// <summary>
        /// Gets metadata for a specific configuration key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>Configuration entry metadata, or null if not found</returns>
        public ConfigurationEntry GetMetadataByKey(string key)
        {
            var metadata = _metadata.FirstOrDefault(m => m.Key == key);
            if (metadata == null)
            {
                return null;
            }

            // Return a copy to prevent external modification
            return new ConfigurationEntry
            {
                Key = metadata.Key,
                Category = metadata.Category,
                Name = metadata.Name,
                Type = metadata.Type,
                DefaultValue = metadata.DefaultValue,
                Description = metadata.Description,
                Value = metadata.Value
            };
        }

        private List<ConfigurationEntry> InitializeMetadata()
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
}

