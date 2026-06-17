using System.Collections.Generic;
using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Transaction.Query;

namespace MindedExample.Application.Transaction.Validator
{
    /// <summary>
    /// Validator for <see cref="GetTransactionsQuery"/>.
    /// Top values exceeding the maximum page size (100) are capped by the handler, not rejected here.
    /// </summary>
    public class GetTransactionsQueryValidator : IQueryValidator<GetTransactionsQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Transaction>>>
    {
        /// <summary>
        /// Validates the query parameters.
        /// </summary>
        /// <param name="query">The query to validate.</param>
        /// <returns>Validation result.</returns>
        public Task<IValidationResult> ValidateAsync(GetTransactionsQuery query)
        {
            return Task.FromResult<IValidationResult>(new ValidationResult());
        }
    }
}
