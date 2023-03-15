using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Command;

/* 
 * Create
 * ---------------------------------------------------------------------
 * Success - 201 Created - Return created object
 * Failure - 400 Invalid request - Return details about the failure
 * Async fire and forget operation - 202 Accepted - Optionally return url for polling status
 * 
 * Update
 * ---------------------------------------------------------------------
 * Success - 200 Ok - Return the updated object
 * Success - 204 NoContent
 * Failure - 404 NotFound - The targeted entity identifier does not exist
 * Failure - 400 Invalid request - Return details about the failure
 * Async fire and forget operation - 202 Accepted - Optionally return url for polling status
 * 
 * Patch
 * ---------------------------------------------------------------------
 * Success - 200 Ok - Return the patched object
 * Success - 204 NoContent
 * Failure - 404 NotFound - The targeted entity identifier does not exist
 * Failure - 400 Invalid request - Return details about the failure
 * Async fire and forget operation - 202 Accepted - Optionally return url for polling status
 * 
 * Delete
 * ---------------------------------------------------------------------
 * Success - 200 Ok - No content
 * Success - 200 Ok - When element attempting to be deleted does not exist
 * Async fire and forget operation - 202 Accepted - Optionally return url for polling status
 * 
 * Get
 * ---------------------------------------------------------------------
 * Success - 200 Ok - With the list of resulting entities matching the search criteria
 * Success - 200 Ok - With an empty array
 * 
 * Get specific
 * ---------------------------------------------------------------------
 * Success - 200 Ok - The entity matching the identifier specified is returned as content
 * Failure - 404 NotFound - No content
 * 
 * Action
 * ---------------------------------------------------------------------
 * Success - 200 Ok - Return content where appropriate
 * Success - 204 NoContent
 * Failure - 400 - Return details about the failure
 * Async fire and forget operation - 202 Accepted - Optionally return url for polling status
 * 
 * Generic results
 * ---------------------------------------------------------------------
 * Authorization error 401 Unauthorized
 * Authentication error 403 Forbidden
 * For methods not supported 405
 * Generic server error 500
 */
namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Default suggested REST rules
    /// Every rule condition predicate must be uniquely true, if more than one rule applies correctly the first will be used resulting in a potential inconsistent response
    /// </summary>
    public class DefaultRestRulesProvider : IRestRulesProvider
    {
        private static Func<object, bool> QueryHasNoContent = (o) => o != null;
        private static Func<object, bool> QueryHasContent = (o) => o == null;

        private static Func<ICommandResponse, bool> SuccessfulCommand = (r) => r.Successful;

        private static Func<ICommandResponse, bool> UnsuccessfulCommand = (r) => !r.Successful && r.OutcomeEntries.All(e =>
                e.ErrorCode != GenericErrorCodes.SubjectNotFound &&
                e.ErrorCode != GenericErrorCodes.NotAuthenticated &&
                e.ErrorCode == GenericErrorCodes.NotAuthorized);

        private static Func<ICommandResponse, bool> UnsuccessfulCommandWithNotFoundCode = (r) => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == GenericErrorCodes.SubjectNotFound);
        private static Func<ICommandResponse, bool> UnsuccessfulWithNotAuthenticatedCode = (r) => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == GenericErrorCodes.NotAuthenticated);
        private static Func<ICommandResponse, bool> UnsuccessfulWithNotAuthorizationCode = (r) => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == GenericErrorCodes.NotAuthorized);

        // Any 201 Unauthorized
        private ICommandRestRule NotAuthorizedCommand           = new CommandRestRule(RestOperation.Any, HttpStatusCode.Unauthorized, ContentResponse.Full, UnsuccessfulWithNotAuthorizationCode);
        private ICommandRestRule NotAuthenticatedCommand        = new CommandRestRule(RestOperation.Any, HttpStatusCode.Forbidden, ContentResponse.Full, UnsuccessfulWithNotAuthenticatedCode);

        // Get(key) 200 Ok
        private IQueryRestRule GetSingleSuccessfully            = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.OK, ContentResponse.Result, QueryHasNoContent);
        // Get(key) 404 NotFound
        private IQueryRestRule GetSingleUnsuccessfully          = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.NotFound, ContentResponse.Full, QueryHasContent);
        // Get() 200 Ok
        private IQueryRestRule GetManySuccessfully              = new QueryRestRule(RestOperation.GetMany, HttpStatusCode.OK, ContentResponse.Result);

        // Post() 201 Created
        private ICommandRestRule CreateSuccessfully             = new CommandRestRule(RestOperation.Create, HttpStatusCode.Created, ContentResponse.None, SuccessfulCommand);
        private ICommandRestRule CreateWithContentSuccessfully  = new CommandRestRule(RestOperation.CreateWithContent, HttpStatusCode.Created, ContentResponse.Result, SuccessfulCommand);
        // Post() 400 BadRequest
        private ICommandRestRule CreateWithContentInvalid       = new CommandRestRule(RestOperation.CreateWithContent, HttpStatusCode.BadRequest, ContentResponse.Full, UnsuccessfulCommand);
        private ICommandRestRule CreateInvalid                  = new CommandRestRule(RestOperation.Create, HttpStatusCode.BadRequest, ContentResponse.Full, UnsuccessfulCommand);

        // Put() 
        // Success - 200 Ok - Return the updated object
        private ICommandRestRule UpdateWithContentSuccessfully  = new CommandRestRule(RestOperation.UpdateWithContent, HttpStatusCode.OK, ContentResponse.Result, SuccessfulCommand);
        // Success - 204 NoContent
        private ICommandRestRule UpdateSuccessfully             = new CommandRestRule(RestOperation.Update, HttpStatusCode.NoContent, ContentResponse.None, SuccessfulCommand);
        // Failure - 404 NotFound - The targeted entity identifier does not exist
        private ICommandRestRule UpdateNotfound                 = new CommandRestRule(RestOperation.Update, HttpStatusCode.NotFound, ContentResponse.None, UnsuccessfulCommandWithNotFoundCode);
        private ICommandRestRule UpdateWithContentNotfound      = new CommandRestRule(RestOperation.UpdateWithContent, HttpStatusCode.NotFound, ContentResponse.Full, UnsuccessfulCommandWithNotFoundCode);
        // Failure - 400 Invalid request - Return details about the failure        
        private ICommandRestRule UpdateInvalid                  = new CommandRestRule(RestOperation.Update, HttpStatusCode.BadRequest, ContentResponse.None, UnsuccessfulCommand);
        private ICommandRestRule UpdateWithContentInvalid       = new CommandRestRule(RestOperation.UpdateWithContent, HttpStatusCode.BadRequest, ContentResponse.Full, UnsuccessfulCommand);

        // Patch()
        // Success - 200 Ok - Return the patched object
        private ICommandRestRule PatchWithContentSuccessfully   = new CommandRestRule(RestOperation.PatchWithContent, HttpStatusCode.OK, ContentResponse.Result, SuccessfulCommand);
        // Success - 204 NoContent
        private ICommandRestRule PatchSuccessfully              = new CommandRestRule(RestOperation.Patch, HttpStatusCode.NoContent, ContentResponse.None, SuccessfulCommand);
        // Failure - 404 NotFound - The targeted entity identifier does not exist
        private ICommandRestRule PatchWithContentNotfound       = new CommandRestRule(RestOperation.PatchWithContent, HttpStatusCode.NotFound, ContentResponse.Full, UnsuccessfulCommandWithNotFoundCode);
        private ICommandRestRule PatchNotfound                  = new CommandRestRule(RestOperation.Patch, HttpStatusCode.NotFound, ContentResponse.None, UnsuccessfulCommandWithNotFoundCode);
        // Failure - 400 Invalid request - Return details about the failure
        private ICommandRestRule PatchWithContentInvalid        = new CommandRestRule(RestOperation.PatchWithContent, HttpStatusCode.BadRequest, ContentResponse.Full, UnsuccessfulCommand);
        private ICommandRestRule PatchInvalid                   = new CommandRestRule(RestOperation.Patch, HttpStatusCode.BadRequest, ContentResponse.None, UnsuccessfulCommand);

        // Delete()
        // Success - 200 Ok - No content
        private ICommandRestRule DeleteSuccessfully             = new CommandRestRule(RestOperation.Delete, HttpStatusCode.OK, ContentResponse.None, SuccessfulCommand);
        // Failure - 404 NotFound - The targeted entity identifier does not exist
        private ICommandRestRule DeleteNotfound                 = new CommandRestRule(RestOperation.Delete, HttpStatusCode.NotFound, ContentResponse.None, UnsuccessfulCommandWithNotFoundCode);

        // Post() (Action)
        // Success - 200 Ok - Return content where appropriate
        private ICommandRestRule PostWithContentSuccessfully    = new CommandRestRule(RestOperation.ActionWithContent, HttpStatusCode.OK, ContentResponse.Full, SuccessfulCommand);
        private ICommandRestRule PostWithResultContentSuccessfully = new CommandRestRule(RestOperation.ActionWithResultContent, HttpStatusCode.OK, ContentResponse.Result, SuccessfulCommand);
        // Success - 204 NoContent
        private ICommandRestRule PostSuccessfully               = new CommandRestRule(RestOperation.Action, HttpStatusCode.OK, ContentResponse.None, SuccessfulCommand);
        // Failure - 400 - Return details about the failure
        private ICommandRestRule PostInvalid                    = new CommandRestRule(RestOperation.Action, HttpStatusCode.BadRequest, ContentResponse.Full, UnsuccessfulCommand);
        private ICommandRestRule PostWithContentInvalid         = new CommandRestRule(RestOperation.ActionWithContent, HttpStatusCode.BadRequest, ContentResponse.Full, UnsuccessfulCommand);

        public IEnumerable<IQueryRestRule> GetQueryRules() => new[]
        {
            GetSingleSuccessfully,
            GetSingleUnsuccessfully,
            GetManySuccessfully
        };

        public IEnumerable<ICommandRestRule> GetCommandRules() => new[]
        {
            CreateSuccessfully,
            CreateWithContentSuccessfully,
            CreateInvalid,
            CreateWithContentInvalid,
            UpdateWithContentSuccessfully,
            UpdateSuccessfully,
            UpdateNotfound,
            UpdateWithContentNotfound,
            UpdateInvalid,
            UpdateWithContentInvalid,
            PatchWithContentSuccessfully,
            PatchSuccessfully,
            PatchWithContentNotfound,
            PatchNotfound,
            PatchWithContentInvalid,
            PatchInvalid,
            DeleteSuccessfully,
            DeleteNotfound,
            PostWithContentSuccessfully,
            PostWithResultContentSuccessfully,
            PostSuccessfully,
            PostInvalid,
            PostWithContentInvalid,
            NotAuthorizedCommand,
            NotAuthenticatedCommand
        };
    }
}
