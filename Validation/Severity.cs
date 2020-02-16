namespace Minded.Validation
{
    /// <summary>
    /// Serverity of a validation entry.
    /// Error causes the <see cref="ValidationResult"/> to have IsValid property false.
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// Error
        /// </summary>
        Error,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Info
        /// </summary>
        Info
    }
}
