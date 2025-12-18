using System;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Extensions.Logging.Configuration
{
    /// <summary>
    /// Configuration options for the logging decorator.
    /// Controls what information is logged for commands and queries.
    /// All properties support both static values and dynamic providers for runtime configuration (e.g., feature flags).
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Gets or sets whether logging is enabled.
        /// When false, the logging decorator is bypassed entirely.
        /// This property is used as the default value when EnabledProvider is not set.
        /// Default: false
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a function that dynamically determines whether logging is enabled.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over Enabled.
        /// The function is called each time a command or query is executed.
        /// Example: () => _featureFlagService.IsEnabled("logging-enabled")
        /// Default: null (uses Enabled instead)
        /// </summary>
        public Func<bool> EnabledProvider { get; set; }

        /// <summary>
        /// Gets or sets whether to log custom message template data from ILoggable implementations.
        /// When true, commands/queries implementing ILoggable will have their custom logging templates included.
        /// This property is used as the default value when LogMessageTemplateDataProvider is not set.
        /// Default: false
        /// </summary>
        public bool LogMessageTemplateData { get; set; }

        /// <summary>
        /// Gets or sets a function that dynamically determines whether to log custom message template data.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over LogMessageTemplateData.
        /// The function is called each time a command or query is executed.
        /// Example: () => _featureFlagService.IsEnabled("logging-template-data")
        /// Default: null (uses LogMessageTemplateData instead)
        /// </summary>
        public Func<bool> LogMessageTemplateDataProvider { get; set; }

        /// <summary>
        /// Gets or sets whether to log outcome entries from command and query responses.
        /// When true, outcome entries will be logged based on the MinimumOutcomeSeverityLevel.
        /// This property is used as the default value when LogOutcomeEntriesProvider is not set.
        /// Default: false
        /// </summary>
        public bool LogOutcomeEntries { get; set; }

        /// <summary>
        /// Gets or sets a function that dynamically determines whether to log outcome entries.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over LogOutcomeEntries.
        /// The function is called each time outcome entries need to be logged.
        /// Example: () => _featureFlagService.IsEnabled("logging-outcome-entries")
        /// Default: null (uses LogOutcomeEntries instead)
        /// </summary>
        public Func<bool> LogOutcomeEntriesProvider { get; set; }

        /// <summary>
        /// Gets or sets the minimum severity level for logging outcome entries.
        /// Only outcome entries with this severity level or higher will be logged.
        /// This property is used as the default value when MinimumOutcomeSeverityLevelProvider is not set.
        /// Default: Severity.Info (logs all outcome entries)
        /// </summary>
        public Severity MinimumOutcomeSeverityLevel { get; set; } = Severity.Info;

        /// <summary>
        /// Gets or sets a function that dynamically determines the minimum severity level for logging outcome entries.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over MinimumOutcomeSeverityLevel.
        /// The function is called each time outcome entries are logged, allowing for dynamic behavior.
        /// Example: () => _featureFlagService.GetSeverityLevel("logging-min-severity")
        /// Default: null (uses MinimumOutcomeSeverityLevel instead)
        /// </summary>
        public Func<Severity> MinimumOutcomeSeverityLevelProvider { get; set; }

        /// <summary>
        /// Gets the effective enabled state for logging.
        /// Uses EnabledProvider if set, otherwise falls back to Enabled.
        /// This method is called each time a command or query is executed.
        /// </summary>
        /// <returns>True if logging is enabled, false otherwise.</returns>
        public bool GetEffectiveEnabled()
        {
            return EnabledProvider?.Invoke() ?? Enabled;
        }

        /// <summary>
        /// Gets the effective state for logging message template data.
        /// Uses LogMessageTemplateDataProvider if set, otherwise falls back to LogMessageTemplateData.
        /// This method is called each time a command or query is executed.
        /// </summary>
        /// <returns>True if message template data should be logged, false otherwise.</returns>
        public bool GetEffectiveLogMessageTemplateData()
        {
            return LogMessageTemplateDataProvider?.Invoke() ?? LogMessageTemplateData;
        }

        /// <summary>
        /// Gets the effective state for logging outcome entries.
        /// Uses LogOutcomeEntriesProvider if set, otherwise falls back to LogOutcomeEntries.
        /// This method is called each time outcome entries need to be logged.
        /// </summary>
        /// <returns>True if outcome entries should be logged, false otherwise.</returns>
        public bool GetEffectiveLogOutcomeEntries()
        {
            return LogOutcomeEntriesProvider?.Invoke() ?? LogOutcomeEntries;
        }

        /// <summary>
        /// Gets the effective minimum severity level for logging outcome entries.
        /// Uses MinimumOutcomeSeverityLevelProvider if set, otherwise falls back to MinimumOutcomeSeverityLevel.
        /// This method is called each time outcome entries need to be filtered.
        /// </summary>
        /// <returns>The minimum severity level to use for filtering outcome entries.</returns>
        public Severity GetEffectiveMinimumSeverityLevel()
        {
            return MinimumOutcomeSeverityLevelProvider?.Invoke() ?? MinimumOutcomeSeverityLevel;
        }
    }
}
