using System.Collections.Generic;

namespace Minded.Extensions.WebApi
{
    public interface IRestRulesProvider
    {
        IEnumerable<IQueryRestRule> GetQueryRules();
        IEnumerable<ICommandRestRule> GetCommandRules();
    }
}
