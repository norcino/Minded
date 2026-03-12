namespace Data.Entity
{
    /// <summary>
    /// Request model for updating a configuration value.
    /// Contains the new value to be applied to a configuration entry.
    /// </summary>
    public class UpdateConfigurationRequest
    {
        /// <summary>
        /// Gets or sets the new value for the configuration.
        /// The type should match the configuration's expected type.
        /// </summary>
        public object Value { get; set; }
    }
}

