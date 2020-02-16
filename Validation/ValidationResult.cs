using System.Collections.Generic;
using System.Linq;

namespace Minded.Validation
{
    /// <summary>
    /// Represent the result of a validation. A valiadion can be valid or invalid, and have multiple <see cref="ValidationEntries"/>, describing the failed validation rules.
    /// </summary>
    public class ValidationResult : IValidationResult
    {
        /// <summary>
        /// Whether validation succeeded
        /// </summary>
        public virtual bool IsValid => ValidationEntries.All(e => e.Severity != Severity.Error);

        /// <summary>
        /// Creates a new validationResult
        /// </summary>
        public ValidationResult()
        {
            this.ValidationEntries = new List<ValidationEntry>();
        }

        /// <summary>
        /// Creates a new ValidationResult from a collection of failures
        /// </summary>
        /// <param name="failures">List of <see cref="ValidationEntry"/> which is later available through <see cref="Entries"/>. This list get's copied.</param>
        /// <remarks>
        /// Every caller is responsible for not adding <c>null</c> to the list.
        /// </remarks>
        public ValidationResult(IEnumerable<ValidationEntry> failures)
        {
            ValidationEntries = failures.Where(failure => failure != null).ToList();
        }

        /// <summary>
        /// A collection of errors
        /// </summary>
        public IList<ValidationEntry> ValidationEntries { get; }

        /// <summary>
        /// Merge two validation results appending the entries of the second validation result to the first
        /// </summary>
        /// <param name="validationResult">Validation result to merge with the current</param>
        /// <returns>Validation result containing al the entries</returns>
        public IValidationResult Merge(IValidationResult validationResult)
        {
            foreach (var validationEntry in validationResult.ValidationEntries)
            {
                ValidationEntries.Add(validationEntry);
            }
            return this;
        }
    }
}