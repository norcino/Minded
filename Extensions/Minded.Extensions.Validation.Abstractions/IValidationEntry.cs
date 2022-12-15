using System.Collections.Generic;

namespace Minded.Extensions.Validation
{
    /// <summary>
    /// Represent a single validation output, potentially also succesful validation results can be returned as part of the validation,
    /// or validation which are considered as warnings and therefore do not cause the overall validation to fail.
    /// </summary>
    public interface IValidationEntry
    {
        /// <summary>
        /// The property value that caused the failure.
        /// </summary>
        object AttemptedValue { get; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        string ErrorCode { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// The name of the property.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// The resource name used for building the message
        /// </summary>
        string ResourceName { get; set; }

        /// <summary>
        /// Custom severity level associated with the failure.
        /// </summary>
        Severity Severity { get; set; }

        /// <summary>
        /// Creates a textual representation of the failure.
        /// </summary>
        string ToString();
    }
}
