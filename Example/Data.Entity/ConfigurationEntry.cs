namespace Data.Entity
{
    /// <summary>
    /// Represents a single configuration entry with metadata.
    /// Used for managing runtime configuration options for Minded decorators.
    /// </summary>
    public class ConfigurationEntry
    {
        /// <summary>
        /// Gets or sets the unique configuration key (e.g., "Logging.Enabled").
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the category/group of the configuration (e.g., "Logging", "Retry").
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the name of the configuration option (e.g., "Enabled", "DefaultRetryCount").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the data type of the configuration value (e.g., "bool", "int", "string").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the current value of the configuration.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the default value of the configuration.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a human-readable description of the configuration option.
        /// </summary>
        public string Description { get; set; }
    }
}

