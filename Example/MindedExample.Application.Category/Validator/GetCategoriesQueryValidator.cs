using System.Collections.Generic;
using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Category.Query;

namespace MindedExample.Application.Category.Validator
{
    public class GetCategoriesQueryValidator : IQueryValidator<GetCategoriesQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Category>>>
    {
        public GetCategoriesQueryValidator()
        {
        }

        public async Task<IValidationResult> ValidateAsync(GetCategoriesQuery query)
        {
            var validationResult = new ValidationResult();
            if(query.Top > 100)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(query.Top), "{0} is above teh maximum allowed 100", GenericErrorCodes.ValidationFailed, Severity.Error));
                return validationResult;
            }
            return validationResult;
        }
    }
}
