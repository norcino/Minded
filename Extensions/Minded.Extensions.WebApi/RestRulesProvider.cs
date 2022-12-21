using System;
using System.Collections.Generic;
using System.Net;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.WebApi
{
    internal class RestRulesProvider : IRestRulesProvider
    {
        private static Func<object, bool> QueryHasNoContent = (o) => o != null;
        private static Func<object, bool> QueryHasContent = (o) => o == null;
        private static Func<ICommandResponse, bool> SuccessfulCommand = (r) => r.Successful;
        private static Func<ICommandResponse, bool> UnsuccessfulCommand = (r) => !r.Successful;

        // Get(key) 200 Ok
        private IQueryRestRule GetSingleSuccessfully = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.OK, true, QueryHasNoContent);
        // Get(key) 404 NotFound
        private IQueryRestRule GetSingleUnsuccessfully = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.NotFound, true, QueryHasContent);
        // Get() 200 Ok
        private IQueryRestRule GetManySuccessfully = new QueryRestRule(RestOperation.GetMany, HttpStatusCode.OK, true);

        // Post() 201 Created
        private ICommandRestRule CreateSuccessfully = new CommandRestRule(RestOperation.Create, HttpStatusCode.Created, false, SuccessfulCommand);
        private ICommandRestRule CreateWithContentSuccessfully = new CommandRestRule(RestOperation.CreateWithContent, HttpStatusCode.Created, true, SuccessfulCommand);
        // Post() 400 BadRequest
        private ICommandRestRule CreateInvalid = new CommandRestRule(RestOperation.Create, HttpStatusCode.BadRequest, false, UnsuccessfulCommand);
        private ICommandRestRule CreateWithContentInvalid = new CommandRestRule(RestOperation.CreateWithContent, HttpStatusCode.BadRequest, true, UnsuccessfulCommand);

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
            CreateWithContentInvalid
        };        
    }
}
