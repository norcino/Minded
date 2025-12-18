using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Microsoft.Extensions.Configuration;

namespace Application.Api.Services
{
    /// <summary>
    /// Thread-safe singleton service for storing and retrieving runtime configuration values.
    /// Provides dynamic configuration that can be changed at runtime without application restart.
    /// All configuration values are stored in memory and changes apply immediately.
    /// Initial values are loaded from appsettings.json if available.
    /// </summary>
    public class RuntimeConfigurationStore
    {
        private readonly ConcurrentDictionary<string, object> _configurations;
        private readonly ReaderWriterLockSlim _lock;
        private readonly IConfiguration _configuration;
        private readonly Guid _guid;

        /// <summary>
        /// Initializes a new instance of the RuntimeConfigurationStore with default values.
        /// Default values are loaded from the decorator options classes and overridden by appsettings.json.
        /// </summary>
        /// <param name="configuration">The application configuration to load initial values from.</param>
        public RuntimeConfigurationStore(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configurations = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _lock = new ReaderWriterLockSlim();
            InitializeDefaults();
            LoadFromConfiguration();
            _guid = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the unique identifier for this instance.
        /// Used for debugging to verify that the same singleton instance is being used.
        /// </summary>
        public Guid InstanceId => _guid;

        /// <summary>
        /// Gets a configuration value by key.
        /// Returns the default value if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="key">The configuration key (e.g., "Logging.Enabled").</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The configuration value or the default value.</returns>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            _lock.EnterReadLock();
            try
            {
                if (_configurations.TryGetValue(key, out var value))
                {
                    return (T)value;
                }
                return defaultValue;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Sets a configuration value by key.
        /// If the key already exists, the value is updated.
        /// </summary>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="key">The configuration key (e.g., "Logging.Enabled").</param>
        /// <param name="value">The configuration value to set.</param>
        public void SetValue<T>(string key, T value)
        {
            _lock.EnterWriteLock();
            try
            {
                _configurations[key] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets all configuration entries as a dictionary.
        /// Returns a snapshot of the current configuration state.
        /// </summary>
        /// <returns>A dictionary containing all configuration key-value pairs.</returns>
        public Dictionary<string, object> GetAllConfigurations()
        {
            _lock.EnterReadLock();
            try
            {
                return new Dictionary<string, object>(_configurations, StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Initializes the configuration store with default values from all decorator options.
        /// These defaults match the default values in the Options classes.
        /// </summary>
        private void InitializeDefaults()
        {
            // System Options (1 option)
            _configurations["System.MinimumLogLevel"] = "Information";

            // Logging Options (4 options)
            _configurations["Logging.Enabled"] = false;
            _configurations["Logging.LogMessageTemplateData"] = false;
            _configurations["Logging.LogOutcomeEntries"] = false;
            _configurations["Logging.MinimumOutcomeSeverityLevel"] = "Info";

            // Exception Options (1 option)
            _configurations["Exception.Serialize"] = true;

            // Retry Options (7 options)
            _configurations["Retry.ApplyToAllQueries"] = false;
            _configurations["Retry.DefaultRetryCount"] = 3;
            _configurations["Retry.DefaultDelay1"] = 0;
            _configurations["Retry.DefaultDelay2"] = 0;
            _configurations["Retry.DefaultDelay3"] = 0;
            _configurations["Retry.DefaultDelay4"] = 0;
            _configurations["Retry.DefaultDelay5"] = 0;

            // DataProtection Options (1 option)
            _configurations["DataProtection.ShowSensitiveData"] = false;

            // Transaction Options (5 options)
            _configurations["Transaction.DefaultTransactionScopeOption"] = TransactionScopeOption.Required.ToString();
            _configurations["Transaction.DefaultIsolationLevel"] = IsolationLevel.ReadCommitted.ToString();
            _configurations["Transaction.DefaultTimeoutSeconds"] = 60;
            _configurations["Transaction.RollbackOnUnsuccessfulResponse"] = true;
            _configurations["Transaction.EnableLogging"] = true;
        }

        /// <summary>
        /// Loads configuration values from appsettings.json.
        /// Values from appsettings.json override the default values.
        /// </summary>
        private void LoadFromConfiguration()
        {
            // Load System Options
            var serilogSection = _configuration.GetSection("Serilog:MinimumLevel");
            if (serilogSection.Exists())
            {
                LoadConfigValue<string>(serilogSection, "Default", "System.MinimumLogLevel");
            }

            // Load Logging Options
            var loggingSection = _configuration.GetSection("Minded:LoggingOptions");
            if (loggingSection.Exists())
            {
                LoadConfigValue<bool>(loggingSection, "Enabled", "Logging.Enabled");
                LoadConfigValue<bool>(loggingSection, "LogMessageTemplateData", "Logging.LogMessageTemplateData");
                LoadConfigValue<bool>(loggingSection, "LogOutcomeEntries", "Logging.LogOutcomeEntries");
                LoadConfigValue<string>(loggingSection, "MinimumOutcomeSeverityLevel", "Logging.MinimumOutcomeSeverityLevel");
            }

            // Load Exception Options
            var exceptionSection = _configuration.GetSection("Minded:ExceptionOptions");
            if (exceptionSection.Exists())
            {
                LoadConfigValue<bool>(exceptionSection, "Serialize", "Exception.Serialize");
            }

            // Load Retry Options
            var retrySection = _configuration.GetSection("Minded:RetryOptions");
            if (retrySection.Exists())
            {
                LoadConfigValue<bool>(retrySection, "ApplyToAllQueries", "Retry.ApplyToAllQueries");
                LoadConfigValue<int>(retrySection, "DefaultRetryCount", "Retry.DefaultRetryCount");
                LoadConfigValue<int>(retrySection, "DefaultDelay1", "Retry.DefaultDelay1");
                LoadConfigValue<int>(retrySection, "DefaultDelay2", "Retry.DefaultDelay2");
                LoadConfigValue<int>(retrySection, "DefaultDelay3", "Retry.DefaultDelay3");
                LoadConfigValue<int>(retrySection, "DefaultDelay4", "Retry.DefaultDelay4");
                LoadConfigValue<int>(retrySection, "DefaultDelay5", "Retry.DefaultDelay5");
            }

            // Load DataProtection Options
            var dataProtectionSection = _configuration.GetSection("Minded:DataProtectionOptions");
            if (dataProtectionSection.Exists())
            {
                LoadConfigValue<bool>(dataProtectionSection, "ShowSensitiveData", "DataProtection.ShowSensitiveData");
            }

            // Load Transaction Options
            var transactionSection = _configuration.GetSection("Minded:TransactionOptions");
            if (transactionSection.Exists())
            {
                LoadConfigValue<string>(transactionSection, "DefaultTransactionScopeOption", "Transaction.DefaultTransactionScopeOption");
                LoadConfigValue<string>(transactionSection, "DefaultIsolationLevel", "Transaction.DefaultIsolationLevel");
                LoadConfigValue<int>(transactionSection, "DefaultTimeoutSeconds", "Transaction.DefaultTimeoutSeconds");
                LoadConfigValue<bool>(transactionSection, "RollbackOnUnsuccessfulResponse", "Transaction.RollbackOnUnsuccessfulResponse");
                LoadConfigValue<bool>(transactionSection, "EnableLogging", "Transaction.EnableLogging");
            }
        }

        /// <summary>
        /// Helper method to load a configuration value from a section.
        /// </summary>
        private void LoadConfigValue<T>(IConfigurationSection section, string key, string targetKey)
        {
            var value = section.GetValue<T>(key);
            if (value != null)
            {
                _configurations[targetKey] = value;
            }
        }
    }
}

