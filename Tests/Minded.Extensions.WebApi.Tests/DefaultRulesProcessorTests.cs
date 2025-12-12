using System.Net;
using AnonymousData;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Minded.Extensions.Exception;
using Minded.Extensions.WebApi;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi.Tests
{
    /// <summary>
    /// Unit tests for DefaultRulesProcessor class.
    /// Tests all ProcessQueryRules and ProcessCommandRules methods with various scenarios.
    /// </summary>
    [TestClass]
    public class DefaultRulesProcessorTests
    {
        private Mock<IRestRulesProvider> _ruleProviderMock;
        private DefaultRulesProcessor _sut;

        [TestInitialize]
        public void Setup()
        {
            _ruleProviderMock = new Mock<IRestRulesProvider>();
            _sut = new DefaultRulesProcessor(_ruleProviderMock.Object);
        }

        #region ProcessQueryRules<T> Tests

        /// <summary>
        /// Tests that ProcessQueryRules returns OkObjectResult with result when rule is null and result is successful.
        /// Verifies fallback behavior when no matching rule is found.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithNullRule_AndSuccessfulResult_ReturnsOkObjectResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.GetSingle;
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(operation, queryResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(queryResult.Result);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns BadRequestObjectResult when rule is null and result is unsuccessful.
        /// Verifies fallback behavior for unsuccessful queries.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithNullRule_AndUnsuccessfulResult_ReturnsBadRequestObjectResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = false };
            RestOperation operation = RestOperation.GetSingle;
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(operation, queryResult);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(queryResult.Result);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns ObjectResult with full response when rule specifies ContentResponse.Full.
        /// Verifies that the entire QueryResponse object is returned.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithRuleContentResponseFull_ReturnsObjectResultWithFullResponse()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.OK, ContentResponse.Full, r => true);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(queryResult);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns ObjectResult with result only when rule specifies ContentResponse.Result.
        /// Verifies that only the Result property is returned, not the full QueryResponse.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithRuleContentResponseResult_ReturnsObjectResultWithResultOnly()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.OK, ContentResponse.Result, r => true);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(queryResult.Result);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns StatusCodeResult when rule specifies ContentResponse.None.
        /// Verifies that no content is returned, only the status code.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithRuleContentResponseNone_ReturnsStatusCodeResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.NoContent, ContentResponse.None, r => true);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, queryResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns NotFound when rule condition matches for null result.
        /// Verifies that rules can properly handle null query results.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithNullResult_AndMatchingRule_ReturnsNotFound()
        {
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.NotFound, ContentResponse.None, r => r == null);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules<string>(operation, null);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        #endregion

        #region ProcessQueryRules (non-generic) Tests

        /// <summary>
        /// Tests that ProcessQueryRules returns OkResult when result is null and no rule matches.
        /// Verifies fallback behavior for null results.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithNullResult_AndNullRule_ReturnsOkResult()
        {
            RestOperation operation = RestOperation.GetSingle;
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(operation, (object)null);

            result.Should().BeOfType<OkResult>();
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns OkObjectResult when result is plain object and no rule matches.
        /// Verifies fallback behavior for plain object results.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithPlainObject_AndNullRule_ReturnsOkObjectResult()
        {
            var plainResult = Any.String();
            RestOperation operation = RestOperation.GetSingle;
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(operation, plainResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(plainResult);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns OkObjectResult when result is IQueryResponse and successful.
        /// Verifies that IQueryResponse is properly handled with Result property extracted.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithSuccessfulIQueryResponse_AndNullRule_ReturnsOkObjectResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.GetSingle;
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(operation, (object)queryResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(queryResult.Result);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns ObjectResult with result when rule matches for plain object.
        /// Verifies that rules work correctly with plain objects.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithPlainObject_AndMatchingRule_ReturnsObjectResult()
        {
            var plainResult = Any.String();
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.OK, ContentResponse.Result, r => r != null);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, plainResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(plainResult);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns ObjectResult with IQueryResponse result when rule matches.
        /// Verifies that IQueryResponse.Result is extracted when ContentResponse is not None.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithIQueryResponse_AndMatchingRule_ReturnsObjectResultWithResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.OK, ContentResponse.Result, r => r != null);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, (object)queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(queryResult.Result);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns StatusCodeResult when rule specifies ContentResponse.None.
        /// Verifies that no content is returned for non-generic version.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithRuleContentResponseNone_ReturnsStatusCodeResult()
        {
            var plainResult = Any.String();
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.NoContent, ContentResponse.None, r => r != null);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, plainResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Tests that ProcessQueryRules returns NotFound when null result matches rule condition.
        /// Verifies that null results can match specific rules.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithNullResult_AndMatchingRule_ReturnsNotFound()
        {
            RestOperation operation = RestOperation.GetSingle;
            var rule = new QueryRestRule(operation, HttpStatusCode.NotFound, ContentResponse.None, r => r == null);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(operation, (object)null);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        #endregion

        #region ProcessCommandRules (non-generic) Tests

        /// <summary>
        /// Tests that ProcessCommandRules returns OkResult when result is null and no rule matches.
        /// Verifies fallback behavior for null command results.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithNullResult_AndNullRule_ReturnsOkResult()
        {
            RestOperation operation = RestOperation.Create;
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(operation, (ICommandResponse)null);

            result.Should().BeOfType<OkResult>();
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns OkObjectResult when result is successful and no rule matches.
        /// Verifies fallback behavior for successful commands.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithSuccessfulResult_AndNullRule_ReturnsOkObjectResult()
        {
            var commandResult = new CommandResponse { Successful = true };
            RestOperation operation = RestOperation.Create;
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns BadRequestObjectResult when result is unsuccessful and no rule matches.
        /// Verifies fallback behavior for unsuccessful commands.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithUnsuccessfulResult_AndNullRule_ReturnsBadRequestObjectResult()
        {
            var commandResult = new CommandResponse { Successful = false };
            RestOperation operation = RestOperation.Create;
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns ObjectResult when rule specifies ContentResponse.Result.
        /// Verifies that the full CommandResponse is returned.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithRuleContentResponseResult_ReturnsObjectResult()
        {
            var commandResult = new CommandResponse { Successful = true };
            RestOperation operation = RestOperation.Create;
            var rule = new CommandRestRule(operation, HttpStatusCode.Created, ContentResponse.Result, r => r.Successful);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(commandResult);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns StatusCodeResult when rule specifies ContentResponse.None.
        /// Verifies that no content is returned, only status code.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithRuleContentResponseNone_ReturnsStatusCodeResult()
        {
            var commandResult = new CommandResponse { Successful = true };
            RestOperation operation = RestOperation.Create;
            var rule = new CommandRestRule(operation, HttpStatusCode.Created, ContentResponse.None, r => r.Successful);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns BadRequest when unsuccessful command matches rule.
        /// Verifies that rules can handle unsuccessful commands.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithUnsuccessfulResult_AndMatchingRule_ReturnsBadRequest()
        {
            var commandResult = new CommandResponse { Successful = false };
            RestOperation operation = RestOperation.Create;
            var rule = new CommandRestRule(operation, HttpStatusCode.BadRequest, ContentResponse.Result, r => !r.Successful);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        #endregion

        #region ProcessCommandRules<T> (generic) Tests

        /// <summary>
        /// Tests that ProcessCommandRules returns OkResult when result is null and no rule matches.
        /// Verifies fallback behavior for null command results.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithNullResult_AndNullRule_ReturnsOkResult()
        {
            RestOperation operation = RestOperation.Create;
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules<string>(operation, null);

            result.Should().BeOfType<OkResult>();
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns OkObjectResult when result is successful and no rule matches.
        /// Verifies fallback behavior for successful commands with result.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithSuccessfulResult_AndNullRule_ReturnsOkObjectResult()
        {
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.Create;
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns BadRequestObjectResult when result is unsuccessful and no rule matches.
        /// Verifies fallback behavior for unsuccessful commands with result.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithUnsuccessfulResult_AndNullRule_ReturnsBadRequestObjectResult()
        {
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = false };
            RestOperation operation = RestOperation.Create;
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns ObjectResult with full response when rule specifies ContentResponse.Full.
        /// Verifies that the entire CommandResponse object is returned.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithRuleContentResponseFull_ReturnsObjectResultWithFullResponse()
        {
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.Create;
            var rule = new CommandRestRule(operation, HttpStatusCode.Created, ContentResponse.Full, r => r.Successful);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(commandResult);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns ObjectResult with result only when rule specifies ContentResponse.Result.
        /// Verifies that only the Result property is returned, not the full CommandResponse.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithRuleContentResponseResult_ReturnsObjectResultWithResultOnly()
        {
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.Create;
            var rule = new CommandRestRule(operation, HttpStatusCode.Created, ContentResponse.Result, r => r.Successful);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(commandResult.Result);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns StatusCodeResult when rule specifies ContentResponse.None.
        /// Verifies that no content is returned, only status code.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithRuleContentResponseNone_ReturnsStatusCodeResult()
        {
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = true };
            RestOperation operation = RestOperation.Create;
            var rule = new CommandRestRule(operation, HttpStatusCode.NoContent, ContentResponse.None, r => r.Successful);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Tests that ProcessCommandRules returns NotFound when unsuccessful command with NotFound error matches rule.
        /// Verifies that rules can handle specific error scenarios.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithNotFoundError_AndMatchingRule_ReturnsNotFound()
        {
            var commandResult = new CommandResponse<string>(Any.String())
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.SubjectNotFound)
                }
            };
            RestOperation operation = RestOperation.Update;
            var rule = new CommandRestRule(operation, HttpStatusCode.NotFound, ContentResponse.None,
                r => !r.Successful && r.OutcomeEntries.Any(e => e.ErrorCode == GenericErrorCodes.SubjectNotFound));
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(operation, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        #endregion

        #region Integration Tests with DefaultRestRulesProvider

        /// <summary>
        /// Tests that ProcessQueryRules works correctly with DefaultRestRulesProvider for successful GetSingle.
        /// Verifies end-to-end integration with real rules provider.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Integration_WithSuccessfulGetSingle_ReturnsOkWithResult()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };

            IActionResult result = processor.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(queryResult.Result);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessQueryRules works correctly with DefaultRestRulesProvider for null GetSingle result.
        /// Verifies that null results return NotFound with full content response (null value).
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Integration_WithNullGetSingleResult_ReturnsNotFound()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);

            IActionResult result = processor.ProcessQueryRules(RestOperation.GetSingle, (object)null);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            objectResult.Value.Should().BeNull();
        }

        /// <summary>
        /// Tests that ProcessQueryRules works correctly with DefaultRestRulesProvider for unsuccessful query with generic error.
        /// Verifies that invalid queries return BadRequest.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Integration_WithUnsuccessfulQuery_ReturnsBadRequest()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);
            var queryResult = new QueryResponse<string>(Any.String())
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, Any.String())
                }
            };

            IActionResult result = processor.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that ProcessQueryRules works correctly with DefaultRestRulesProvider for plain object result.
        /// Verifies that plain objects (non-IQueryResponse) are handled correctly.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Integration_WithPlainObjectResult_ReturnsOk()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);
            var plainResult = Any.String();

            IActionResult result = processor.ProcessQueryRules(RestOperation.GetMany, plainResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.Value.Should().Be(plainResult);
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessCommandRules works correctly with DefaultRestRulesProvider for successful Create.
        /// Verifies that successful creates return Created status.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Integration_WithSuccessfulCreate_ReturnsCreated()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);
            var commandResult = new CommandResponse { Successful = true };

            IActionResult result = processor.ProcessCommandRules(RestOperation.Create, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Tests that ProcessCommandRules works correctly with DefaultRestRulesProvider for unsuccessful Update with NotFound.
        /// Verifies that NotFound errors return NotFound status.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Integration_WithNotFoundUpdate_ReturnsNotFound()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);
            var commandResult = new CommandResponse
            {
                Successful = false,
                OutcomeEntries = new List<IOutcomeEntry>
                {
                    new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error, GenericErrorCodes.SubjectNotFound)
                }
            };

            IActionResult result = processor.ProcessCommandRules(RestOperation.Update, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests that ProcessCommandRules works correctly with DefaultRestRulesProvider for generic command with result.
        /// Verifies that commands with results return the result value.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Integration_WithSuccessfulCommandWithResult_ReturnsCreatedWithResult()
        {
            var realProvider = new DefaultRestRulesProvider();
            var processor = new DefaultRulesProcessor(realProvider);
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = true };

            IActionResult result = processor.ProcessCommandRules(RestOperation.Create, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        #endregion

        #region Bug Fix Verification Tests

        /// <summary>
        /// Tests that ProcessQueryRules generic version handles null result gracefully when no rule matches.
        /// This test verifies the fix for Bug #1 where result.Successful was accessed after checking result == null.
        /// After the fix, null results should return OkResult as default fallback.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithNullResult_AndNoRules_ReturnsOkResult()
        {
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules<string>(RestOperation.GetSingle, null);

            result.Should().BeOfType<OkResult>();
        }

        /// <summary>
        /// Tests that ProcessQueryRules non-generic version handles unsuccessful IQueryResponse when no rule matches.
        /// This test verifies the fix for Bug #2 where rule.ResultStatusCode was accessed when rule was null.
        /// After the fix, unsuccessful IQueryResponse should return BadRequestObjectResult with the Result property.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithUnsuccessfulIQueryResponse_AndNoRules_ReturnsBadRequestObjectResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = false };
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, (object)queryResult);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(queryResult.Result);
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests that ProcessQueryRules generic version handles IQueryResponse with null Result property.
        /// Verifies that null Result values are handled correctly when the response is successful.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithSuccessfulResult_ButNullResultProperty_ReturnsOkObjectResultWithNull()
        {
            var queryResult = new QueryResponse<string>((string)null) { Successful = true };
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeNull();
        }

        /// <summary>
        /// Tests that ProcessQueryRules non-generic version handles IQueryResponse with null Result property.
        /// Verifies that null Result values are extracted correctly from IQueryResponse objects.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithSuccessfulIQueryResponse_ButNullResultProperty_ReturnsOkObjectResultWithNull()
        {
            var queryResult = new QueryResponse<string>((string)null) { Successful = true };
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, (object)queryResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeNull();
        }

        /// <summary>
        /// Tests that ProcessCommandRules generic version handles ICommandResponse with null Result property.
        /// Verifies that null Result values are handled correctly when the response is successful.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithSuccessfulResult_ButNullResultProperty_ReturnsOkObjectResultWithResponse()
        {
            var commandResult = new CommandResponse<string>((string)null) { Successful = true };
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(RestOperation.Create, commandResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessQueryRules handles multiple matching rules by returning the first match.
        /// Verifies that rule precedence works correctly when multiple rules could match.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithMultipleMatchingRules_ReturnsFirstMatch()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            var firstRule = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.OK, ContentResponse.Result, (o) => true);
            var secondRule = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.Accepted, ContentResponse.Full, (o) => true);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { firstRule, secondRule });

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            objectResult.Value.Should().Be(queryResult.Result);
        }

        /// <summary>
        /// Tests that ProcessCommandRules handles multiple matching rules by returning the first match.
        /// Verifies that rule precedence works correctly for command operations.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithMultipleMatchingRules_ReturnsFirstMatch()
        {
            var commandResult = new CommandResponse { Successful = true };
            var firstRule = new CommandRestRule(RestOperation.Create, HttpStatusCode.Created, ContentResponse.None, (o) => true);
            var secondRule = new CommandRestRule(RestOperation.Create, HttpStatusCode.OK, ContentResponse.Result, (o) => true);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { firstRule, secondRule });

            IActionResult result = _sut.ProcessCommandRules(RestOperation.Create, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Tests that ProcessQueryRules handles rules with null RuleCondition predicate.
        /// Verifies that rules without conditions always match.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithRuleHavingNullCondition_MatchesAnyResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = false };
            var rule = new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.NotFound, ContentResponse.None, null);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests that ProcessCommandRules handles rules with null RuleCondition predicate.
        /// Verifies that command rules without conditions always match.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithRuleHavingNullCondition_MatchesAnyResult()
        {
            var commandResult = new CommandResponse<int>(Any.Int()) { Successful = true };
            var rule = new CommandRestRule(RestOperation.Update, HttpStatusCode.NoContent, ContentResponse.None, null);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(RestOperation.Update, commandResult);

            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Tests that ProcessQueryRules handles empty rule list from provider.
        /// Verifies that empty rule collections are handled gracefully.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_NonGeneric_WithEmptyRuleList_ReturnsDefaultOkResult()
        {
            var plainObject = new { Name = Any.String(), Value = Any.Int() };
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule>());

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, plainObject);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(plainObject);
        }

        /// <summary>
        /// Tests that ProcessCommandRules handles empty rule list from provider.
        /// Verifies that empty command rule collections are handled gracefully.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithEmptyRuleList_ReturnsDefaultOkObjectResult()
        {
            var commandResult = new CommandResponse { Successful = true };
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule>());

            IActionResult result = _sut.ProcessCommandRules(RestOperation.Create, commandResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessQueryRules handles null rule provider gracefully.
        /// Verifies that null provider returns default fallback behavior.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithNullRuleProvider_ReturnsDefaultOkObjectResult()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns((List<IQueryRestRule>)null);

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(queryResult.Result);
        }

        /// <summary>
        /// Tests that ProcessCommandRules handles null rule provider gracefully.
        /// Verifies that null command rule provider returns default fallback behavior.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_Generic_WithNullRuleProvider_ReturnsDefaultOkObjectResult()
        {
            var commandResult = new CommandResponse<string>(Any.String()) { Successful = true };
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns((List<ICommandRestRule>)null);

            IActionResult result = _sut.ProcessCommandRules(RestOperation.Create, commandResult);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(commandResult);
        }

        /// <summary>
        /// Tests that ProcessQueryRules handles RestOperation.Any correctly.
        /// Verifies that wildcard operations match appropriately.
        /// </summary>
        [TestMethod]
        public void ProcessQueryRules_Generic_WithRestOperationAny_MatchesRule()
        {
            var queryResult = new QueryResponse<string>(Any.String()) { Successful = true };
            var rule = new QueryRestRule(RestOperation.Any, HttpStatusCode.OK, ContentResponse.Result, (o) => true);
            _ruleProviderMock.Setup(rp => rp.GetQueryRules()).Returns(new List<IQueryRestRule> { rule });

            IActionResult result = _sut.ProcessQueryRules(RestOperation.GetSingle, queryResult);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests that ProcessCommandRules handles combined RestOperation flags correctly.
        /// Verifies that bitwise flag operations work as expected with HasFlag.
        /// Note: HasFlag checks if the operation parameter has the rule's flag, so when rule has
        /// Create|Update and we pass Create, HasFlag(Create|Update) returns false because Create
        /// doesn't have both flags. This test verifies the actual behavior.
        /// </summary>
        [TestMethod]
        public void ProcessCommandRules_NonGeneric_WithCombinedRestOperationFlags_DoesNotMatch()
        {
            var commandResult = new CommandResponse { Successful = true };
            var rule = new CommandRestRule(RestOperation.Create | RestOperation.Update, HttpStatusCode.Accepted, ContentResponse.Result, (o) => true);
            _ruleProviderMock.Setup(rp => rp.GetCommandRules()).Returns(new List<ICommandRestRule> { rule });

            IActionResult result = _sut.ProcessCommandRules(RestOperation.Create, commandResult);

            // Since Create.HasFlag(Create|Update) is false, no rule matches, falls back to default
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(commandResult);
        }

        #endregion
    }
}

