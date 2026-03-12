namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Describes a log enabled object which can be converted to a template and list of parameters.
    /// Use LoggingProperties to specify property paths like "User.Email", "Category.Name" for automatic
    /// extraction and sanitization of sensitive data.
    /// </summary>
    public interface ILoggable
    {
        /// <summary>
        /// Template to be used for string interpolation.
        /// Uses Serilog-style placeholders (e.g., "Creating user {Email} with name {Name}").
        /// </summary>
        string LoggingTemplate { get; }

        /// <summary>
        /// Property paths to extract from the command/query for logging.
        /// Supports dot notation for nested properties (e.g., "User.Email", "Order.Customer.Name").
        /// Properties marked with [SensitiveData] are automatically masked based on DataProtection configuration.
        /// All logging data must go through this property to ensure proper sanitization.
        /// </summary>
        /// <example>
        /// <code>
        /// public string[] LoggingProperties => new[] { "UserId", "User.Email", "Category.Name" };
        /// </code>
        /// </example>
        string[] LoggingProperties { get; }
    }
}
