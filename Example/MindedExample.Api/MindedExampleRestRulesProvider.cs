using System.Collections.Generic;
using System.Linq;
using System.Net;
using Minded.Extensions.Exception;
using Minded.Extensions.WebApi;
using MindedExample.Application.User;

namespace MindedExample.Api
{
    /// <summary>
    /// Extends the default REST rules with application-specific HTTP status code mappings.
    /// Adds HTTP 409 Conflict responses for <see cref="AuthErrorCodes.EmailAlreadyExists"/>
    /// and <see cref="AuthErrorCodes.PendingJoinRequestExists"/>.
    /// </summary>
    public class MindedExampleRestRulesProvider : IRestRulesProvider
    {
        private readonly DefaultRestRulesProvider _default = new DefaultRestRulesProvider();

        private static readonly ICommandRestRule ConflictEmailRule = new CommandRestRule(
            RestOperation.Any,
            HttpStatusCode.Conflict,
            ContentResponse.Full,
            r => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == AuthErrorCodes.EmailAlreadyExists));

        private static readonly ICommandRestRule ConflictJoinRequestRule = new CommandRestRule(
            RestOperation.Any,
            HttpStatusCode.Conflict,
            ContentResponse.Full,
            r => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == AuthErrorCodes.PendingJoinRequestExists));

        /// <summary>
        /// Catches SubjectNotFound errors on Action operations (e.g. register join-tenant with non-existent tenant)
        /// where the default rules only add NotFound mapping for Update/Patch/Delete.
        /// </summary>
        private static readonly ICommandRestRule NotFoundRule = new CommandRestRule(
            RestOperation.Any,
            HttpStatusCode.NotFound,
            ContentResponse.Full,
            r => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == GenericErrorCodes.SubjectNotFound));

        /// <inheritdoc />
        public IEnumerable<ICommandRestRule> GetCommandRules()
        {
            // Custom rules evaluated first; first match wins.
            yield return ConflictEmailRule;
            yield return ConflictJoinRequestRule;
            yield return NotFoundRule;
            foreach (var rule in _default.GetCommandRules())
                yield return rule;
        }

        /// <inheritdoc />
        public IEnumerable<IQueryRestRule> GetQueryRules() => _default.GetQueryRules();
    }
}
