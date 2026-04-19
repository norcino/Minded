using System.Collections.Generic;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Provides the sets of REST rules used by <see cref="IRulesProcessor"/> to map command and query
    /// outcomes to HTTP responses.
    /// Implement this interface to define custom HTTP-response mappings, or use the built-in
    /// <see cref="DefaultRestRulesProvider"/>.
    /// </summary>
    public interface IRestRulesProvider
    {
        /// <summary>
        /// Returns the ordered collection of query REST rules evaluated against query results.
        /// Rules are evaluated in order; the first matching rule wins.
        /// </summary>
        IEnumerable<IQueryRestRule> GetQueryRules();

        /// <summary>
        /// Returns the ordered collection of command REST rules evaluated against command responses.
        /// Rules are evaluated in order; the first matching rule wins.
        /// </summary>
        IEnumerable<ICommandRestRule> GetCommandRules();
    }
}
