using System.Collections.Generic;
using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Service.Category.Query;

namespace Service.Category.Validator
{
    public class GetCategoriesQueryValidator : IQueryValidator<GetCategoriesQuery, IQueryResponse<IEnumerable<Data.Entity.Category>>>
    {
        private readonly IValidator<Data.Entity.Category> _categoryValidator;

        public GetCategoriesQueryValidator(IValidator<Data.Entity.Category> categoryValidator)
        {
            _categoryValidator = categoryValidator;
        }

        public async Task<IValidationResult> ValidateAsync(GetCategoriesQuery query)
        {
            var validationResult = new ValidationResult();
            if(query.Top > 100)
            {
                validationResult.OutcomeEntries.Add(new OutcomeEntry(nameof(query.Top), "{0} is above teh maximum allowed 100"));
                return validationResult;
            }
            return validationResult;
        }
    }
}
