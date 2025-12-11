using System.Net;
using AnonymousData;
using FluentAssertions;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi.Tests
{
    /// <summary>
    /// Unit tests for DefaultRestRulesProvider class.
    /// Tests both the structure of rules and the behavior of rule condition predicates.
    /// </summary>
    [TestClass]
    public class DefaultRestRulesProviderTests
    {
        private DefaultRestRulesProvider _sut;

        [TestInitialize]
        public void Setup()
        {
            _sut = new DefaultRestRulesProvider();
        }

        #region Query Rules Structure Tests

        /// <summary>
        /// Tests that GetQueryRules returns the expected number and types of rules.
        /// Verifies all query rules are present with correct operation, status code, and content response.
        /// </summary>
        [TestMethod]
        public void GetQueryRules_ReturnsExpectedRules()
        {
            var rules = _sut.GetQueryRules().ToList();

            rules.Should().HaveCount(6);
            rules.Should().Contain(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result);
            rules.Should().Contain(r => r.Operation == RestOperation.GetMany && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result);
            rules.Should().Contain(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.AnyGet && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
        }

        #endregion

        #region Command Rules Structure Tests

        /// <summary>
        /// Tests that GetCommandRules returns the expected number and types of rules.
        /// Verifies all command rules are present with correct operation, status code, and content response.
        /// </summary>
        [TestMethod]
        public void GetCommandRules_ReturnsExpectedRules()
        {
            var rules = _sut.GetCommandRules().ToList();

            rules.Should().HaveCount(25);
            rules.Should().Contain(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.Created && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.CreateWithContent && r.ResultStatusCode == HttpStatusCode.Created && r.ContentResponse == ContentResponse.Result);
            rules.Should().Contain(r => r.Operation == RestOperation.CreateWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.UpdateWithContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result);
            rules.Should().Contain(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.NoContent && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.UpdateWithContent && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.UpdateWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.PatchWithContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result);
            rules.Should().Contain(r => r.Operation == RestOperation.Patch && r.ResultStatusCode == HttpStatusCode.NoContent && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.PatchWithContent && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Patch && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.PatchWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Patch && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.Delete && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.Delete && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.ActionWithContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.ActionWithResultContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result);
            rules.Should().Contain(r => r.Operation == RestOperation.Action && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.None);
            rules.Should().Contain(r => r.Operation == RestOperation.Action && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.ActionWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized && r.ContentResponse == ContentResponse.Full);
            rules.Should().Contain(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden && r.ContentResponse == ContentResponse.Full);
        }

        #endregion

        #region Command Rule Condition Tests

        /// <summary>
        /// Tests that successful command rule condition returns true for successful command response.
        /// Verifies the SuccessfulCommand predicate behavior.
        /// </summary>
        [TestMethod]
        public void CommandRuleCondition_SuccessfulCommand_ReturnsTrue()
        {
            var response = new CommandResponse { Successful = true };
            ICommandRestRule rule = _sut.GetCommandRules().First(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.Created);

            var result = rule.RuleCondition(response);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that unsuccessful command rule condition returns true for unsuccessful command without special error codes.
        /// Verifies the UnsuccessfulCommand predicate behavior.
        /// </summary>
        [TestMethod]
        public void CommandRuleCondition_UnsuccessfulCommand_WithGenericError_ReturnsTrue()
        {
            var response = new CommandResponse
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, Any.String())
                }
            };
            ICommandRestRule rule = _sut.GetCommandRules().First(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.BadRequest);

            var result = rule.RuleCondition(response);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that unsuccessful command rule condition returns false when error code is NotFound.
        /// Verifies the UnsuccessfulCommand predicate excludes NotFound errors.
        /// </summary>
        [TestMethod]
        public void CommandRuleCondition_UnsuccessfulCommand_WithNotFoundError_ReturnsFalse()
        {
            var response = new CommandResponse
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.SubjectNotFound)
                }
            };
            ICommandRestRule rule = _sut.GetCommandRules().First(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.BadRequest);

            var result = rule.RuleCondition(response);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that NotFound command rule condition returns true when error code is SubjectNotFound.
        /// Verifies the UnsuccessfulCommandWithNotFoundCode predicate behavior.
        /// </summary>
        [TestMethod]
        public void CommandRuleCondition_NotFoundCommand_WithNotFoundError_ReturnsTrue()
        {
            var response = new CommandResponse
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.SubjectNotFound)
                }
            };
            ICommandRestRule rule = _sut.GetCommandRules().First(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.NotFound);

            var result = rule.RuleCondition(response);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that NotAuthenticated command rule condition returns true when error code is NotAuthenticated.
        /// Verifies the UnsuccessfulCommandWithNotAuthenticatedCode predicate behavior.
        /// </summary>
        [TestMethod]
        public void CommandRuleCondition_NotAuthenticatedCommand_WithNotAuthenticatedError_ReturnsTrue()
        {
            var response = new CommandResponse
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.NotAuthenticated)
                }
            };
            ICommandRestRule rule = _sut.GetCommandRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden);

            var result = rule.RuleCondition(response);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that NotAuthorized command rule condition returns true when error code is NotAuthorized.
        /// Verifies the UnsuccessfulCommandWithNotAuthorizationCode predicate behavior.
        /// </summary>
        [TestMethod]
        public void CommandRuleCondition_NotAuthorizedCommand_WithNotAuthorizedError_ReturnsTrue()
        {
            var response = new CommandResponse
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.NotAuthorized)
                }
            };
            ICommandRestRule rule = _sut.GetCommandRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized);

            var result = rule.RuleCondition(response);

            result.Should().BeTrue();
        }

        #endregion

        #region Query Rule Condition Tests

        /// <summary>
        /// Tests that query rule condition returns true when query result is null.
        /// Verifies the QueryHasNoContent predicate behavior.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetSingleUnsuccessfully_WithNullResult_ReturnsTrue()
        {
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.NotFound);

            var result = rule.RuleCondition(null);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that successful query rule condition returns true when query result has content and is successful.
        /// Verifies the GetSingleSuccessfully predicate behavior with QueryResponse.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetSingleSuccessfully_WithSuccessfulQueryResponse_ReturnsTrue()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.OK);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that successful query rule condition returns false when query result is null.
        /// Verifies the GetSingleSuccessfully predicate behavior with null content.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetSingleSuccessfully_WithNullResult_ReturnsFalse()
        {
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.OK);

            var result = rule.RuleCondition(null);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that successful query rule condition returns true for non-IMessageResponse objects.
        /// Verifies the SuccessfulQuery predicate behavior with plain objects.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetManySuccessfully_WithPlainObject_ReturnsTrue()
        {
            var queryResult = Any.String();
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.GetMany && r.ResultStatusCode == HttpStatusCode.OK);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that successful query rule condition returns true when QueryResponse is successful.
        /// Verifies the SuccessfulQuery predicate behavior.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetManySuccessfully_WithSuccessfulQueryResponse_ReturnsTrue()
        {
            var queryResult = new QueryResponse<List<string>>(new List<string> { Any.String() }) { Successful = true };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.GetMany && r.ResultStatusCode == HttpStatusCode.OK);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that successful query rule condition returns false when QueryResponse is unsuccessful.
        /// Verifies the SuccessfulQuery predicate behavior with unsuccessful response.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetManySuccessfully_WithUnsuccessfulQueryResponse_ReturnsFalse()
        {
            var queryResult = new QueryResponse<List<string>>(new List<string>()) { Successful = false };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.GetMany && r.ResultStatusCode == HttpStatusCode.OK);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that invalid query rule condition returns false when query result is null.
        /// Verifies the InvalidQuery predicate behavior with null.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetInvalid_WithNullResult_ReturnsFalse()
        {
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.AnyGet && r.ResultStatusCode == HttpStatusCode.BadRequest);

            var result = rule.RuleCondition(null);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that invalid query rule condition returns true for non-IMessageResponse objects.
        /// Verifies the InvalidQuery predicate behavior with plain objects.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetInvalid_WithPlainObject_ReturnsTrue()
        {
            var queryResult = Any.String();
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.AnyGet && r.ResultStatusCode == HttpStatusCode.BadRequest);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that invalid query rule condition returns true when QueryResponse is unsuccessful with generic error.
        /// Verifies the InvalidQuery predicate behavior with unsuccessful QueryResponse.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetInvalid_WithUnsuccessfulQueryResponse_ReturnsTrue()
        {
            var queryResult = new QueryResponse<string>(Any.String())
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, Any.String())
                }
            };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.AnyGet && r.ResultStatusCode == HttpStatusCode.BadRequest);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that invalid query rule condition returns false when QueryResponse has NotFound error code.
        /// Verifies the InvalidQuery predicate excludes NotFound errors.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_GetInvalid_WithNotFoundError_ReturnsFalse()
        {
            var queryResult = new QueryResponse<string>(Any.String())
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.SubjectNotFound)
                }
            };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.AnyGet && r.ResultStatusCode == HttpStatusCode.BadRequest);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that NotAuthenticated query rule condition returns true when error code is NotAuthenticated.
        /// Verifies the UnsuccessfulQueryWithNotAuthenticatedCode predicate behavior.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_NotAuthenticatedQuery_WithNotAuthenticatedError_ReturnsTrue()
        {
            var queryResult = new QueryResponse<string>(Any.String())
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.NotAuthenticated)
                }
            };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that NotAuthenticated query rule condition returns false when result is null.
        /// Verifies the UnsuccessfulQueryWithNotAuthenticatedCode predicate behavior with null.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_NotAuthenticatedQuery_WithNullResult_ReturnsFalse()
        {
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden);

            var result = rule.RuleCondition(null);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that NotAuthenticated query rule condition returns true for non-IMessageResponse objects.
        /// Verifies the UnsuccessfulQueryWithNotAuthenticatedCode predicate behavior with plain objects.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_NotAuthenticatedQuery_WithPlainObject_ReturnsTrue()
        {
            var queryResult = Any.String();
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that NotAuthorized query rule condition returns true when error code is NotAuthorized.
        /// Verifies the UnsuccessfulQueryWithNotAuthorizationCode predicate behavior.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_NotAuthorizedQuery_WithNotAuthorizedError_ReturnsTrue()
        {
            var queryResult = new QueryResponse<string>(Any.String())
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.NotAuthorized)
                }
            };
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that NotAuthorized query rule condition returns false when result is null.
        /// Verifies the UnsuccessfulQueryWithNotAuthorizationCode predicate behavior with null.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_NotAuthorizedQuery_WithNullResult_ReturnsFalse()
        {
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized);

            var result = rule.RuleCondition(null);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that NotAuthorized query rule condition returns true for non-IMessageResponse objects.
        /// Verifies the UnsuccessfulQueryWithNotAuthorizationCode predicate behavior with plain objects.
        /// </summary>
        [TestMethod]
        public void QueryRuleCondition_NotAuthorizedQuery_WithPlainObject_ReturnsTrue()
        {
            var queryResult = Any.String();
            IQueryRestRule rule = _sut.GetQueryRules().First(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized);

            var result = rule.RuleCondition(queryResult);

            result.Should().BeTrue();
        }

        #endregion
    }
}
