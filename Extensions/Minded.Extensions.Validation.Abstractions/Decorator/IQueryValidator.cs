using System.Threading.Tasks;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Validation.Decorator
{
    public interface IQueryValidator<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<IValidationResult> ValidateAsync(TQuery query);
    }
}
