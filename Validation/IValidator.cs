using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minded.Validation
{
    /// <summary>
    /// Generic Validator inverface which can be used to validate an input class using ValidateAsync, and get <see cref="IValidationResult"/> as result of the validation.
    /// </summary>
    /// <typeparam name="T">Type for which the validator has been designed</typeparam>
    public interface IValidator<in T> where T : class
    {
        /// <summary>
        /// Validate asyncronously the input object, returning <see cref="IValidationResult"/>
        /// </summary>
        /// <param name="subject">Subject of the validation</param>
        /// <returns>Result of the validation as <see cref="IValidationResult"/></returns>
        Task<IValidationResult> ValidateAsync(T subject);
    }
}
