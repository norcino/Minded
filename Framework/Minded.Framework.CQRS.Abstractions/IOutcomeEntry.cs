namespace Minded.Framework.CQRS.Abstractions
{
    /// <summary>
    /// Represents a single detail output, associated to a validation or a processing.
    /// Outcomes are not necessarily related to failures or warnings but can be used to provide additional information about the completed processing.
    /// </summary>
    public interface IOutcomeEntry
    {
        /// <summary>
        /// The property value that caused the alert or failure
        /// </summary>
        object AttemptedValue { get; }

        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        string ErrorCode { get; set; }

        /// <summary>
        /// The details message
        /// </summary>
        string Message { get; }

        /// <summary>
        /// The name of the property
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// The resource name used for building the message
        /// </summary>
        string ResourceName { get; set; }

        /// <summary>
        /// Custom severity level associated with the outcome entry
        /// </summary>
        Severity Severity { get; set; }

        /// <summary>
        /// Creates a textual representation of the failure
        /// </summary>
        string ToString();
    }
}
