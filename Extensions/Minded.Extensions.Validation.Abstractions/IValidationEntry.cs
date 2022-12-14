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
        /// Custom state associated with the failure.
        /// </summary>
        object CustomState { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        string ErrorCode { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// Gets or sets the formatted message arguments.
        /// These are values for custom formatted message in validator resource files
        /// Same formatted message can be reused in UI and with same number of format placeholders
        /// Like "Value {0} that you entered should be {1}"
        /// </summary>
        object[] FormattedMessageArguments { get; set; }

        /// <summary>
        /// Gets or sets the formatted message placeholder values.
        /// Similar placeholders are defined in fluent validation library (check documentation)
        /// </summary>
        Dictionary<string, object> FormattedMessagePlaceholderValues { get; set; }

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
