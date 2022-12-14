using System.Collections.Generic;

namespace Minded.Extensions.Validation
{
    /// <summary>
    /// Interface representing the result of a validation.
    /// </summary>
    public interface IValidationResult
    {
        /// <summary>
        /// True if the validation passed without errors (Warnings are not considered a failure)
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// List of all <see cref="ValidationEntries"/> result of the validation
        /// </summary>
        IList<IValidationEntry> ValidationEntries { get; }

        /// <summary>
        /// Allows to merge two different validation results, if one is not valid the resulting <see cref="IValidationResult"/> will be not valid
        /// </summary>
        /// <param name="validationResult">Validation result to merge with</param>
        /// <returns><see cref="IValidationResult"/> with the entries of both results</returns>
        IValidationResult Merge(IValidationResult validationResult);
    }
}
