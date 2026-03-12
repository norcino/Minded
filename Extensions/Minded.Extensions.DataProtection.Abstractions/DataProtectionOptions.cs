using System;

namespace Minded.Extensions.DataProtection.Abstractions
{
    /// <summary>
    /// Configuration options for data protection and sensitive data handling.
    /// Controls whether properties marked with [SensitiveData] attribute are shown or hidden
    /// in logs, exception messages, and other output.
    /// </summary>
    /// <remarks>
    /// By default, sensitive data is hidden (ShowSensitiveData = false) to ensure compliance
    /// with data protection regulations (GDPR, CCPA, etc.) and prevent accidental exposure
    /// of sensitive information.
    /// 
    /// Use ShowSensitiveDataProvider for dynamic runtime configuration based on environment,
    /// feature flags, user roles, or other runtime conditions.
    /// </remarks>
    public class DataProtectionOptions
    {
        /// <summary>
        /// Gets or sets whether sensitive data should be shown in logs and exception messages.
        /// Default is false (hide sensitive data).
        /// </summary>
        /// <remarks>
        /// When false (default):
        /// - Properties marked with [SensitiveData] are omitted from logs and exception messages
        /// - Ensures GDPR/CCPA compliance
        /// - Recommended for production environments
        /// 
        /// When true:
        /// - All properties are included, even those marked with [SensitiveData]
        /// - Useful for development and debugging
        /// - Should only be enabled in secure, non-production environments
        /// 
        /// If ShowSensitiveDataProvider is set, it takes precedence over this static value.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Static configuration
        /// options.ShowSensitiveData = false; // Hide sensitive data (default)
        /// </code>
        /// </example>
        public bool ShowSensitiveData { get; set; } = false;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether sensitive data should be shown.
        /// This provider is invoked each time data needs to be sanitized, allowing for runtime configuration.
        /// If set, this takes precedence over the static ShowSensitiveData property.
        /// </summary>
        /// <remarks>
        /// Use this for dynamic configuration based on:
        /// - Environment (Development vs Production)
        /// - Feature flags
        /// - User roles or permissions
        /// - Configuration values
        /// - Any other runtime condition
        /// 
        /// The provider function should be fast and thread-safe as it may be called frequently.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Show sensitive data only in development environment
        /// options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
        /// 
        /// // Show sensitive data based on feature flag
        /// options.ShowSensitiveDataProvider = () => _featureFlags.IsEnabled("ShowSensitiveData");
        /// 
        /// // Show sensitive data based on configuration
        /// options.ShowSensitiveDataProvider = () => _configuration.GetValue&lt;bool&gt;("DataProtection:ShowSensitiveData");
        /// 
        /// // Show sensitive data only for admin users
        /// options.ShowSensitiveDataProvider = () => _currentUser.IsAdmin;
        /// </code>
        /// </example>
        public Func<bool> ShowSensitiveDataProvider { get; set; }

        /// <summary>
        /// Gets the effective setting for showing sensitive data.
        /// Uses ShowSensitiveDataProvider if set, otherwise falls back to ShowSensitiveData.
        /// This method is called each time sensitive data needs to be sanitized.
        /// </summary>
        /// <returns>True if sensitive data should be shown, false if it should be hidden.</returns>
        public bool GetEffectiveShowSensitiveData()
        {
            return ShowSensitiveDataProvider?.Invoke() ?? ShowSensitiveData;
        }
    }
}

