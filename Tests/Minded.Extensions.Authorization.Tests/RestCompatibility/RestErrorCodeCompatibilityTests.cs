using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Minded.Extensions.Authorization;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Authorization.Decorator;
using Minded.Extensions.Exception;
using Minded.Extensions.WebApi;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Authorization.Tests.RestCompatibility;

#region Test Command Types

[RequireRoles("Admin")]
public class RestTestProtectedCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

#endregion

[TestClass]
public class RestErrorCodeCompatibilityTests
{
    private static readonly string[] RolePool = { "Admin", "Manager", "Editor", "Viewer", "SuperAdmin" };

    #region Helpers

    private static AuthorizationCommandHandlerDecorator<TCommand> CreateDecorator<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IAuthorizationContextAccessor contextAccessor,
        IRequestAuthorizationEvaluator evaluator = null,
        AuthorizationOptions options = null) where TCommand : ICommand
    {
        evaluator ??= new DefaultRequestAuthorizationEvaluator();
        options ??= new AuthorizationOptions();
        var optionsWrapper = Options.Create(options);
        var logger = NullLoggerFactory.Instance.CreateLogger<AuthorizationCommandHandlerDecorator<TCommand>>();

        return new AuthorizationCommandHandlerDecorator<TCommand>(
            innerHandler, contextAccessor, evaluator, optionsWrapper, logger);
    }

    private static Mock<ICommandHandler<TCommand>> CreateMockHandler<TCommand>(ICommandResponse response = null) where TCommand : ICommand
    {
        var mock = new Mock<ICommandHandler<TCommand>>();
        response ??= CommandResponse.Success();
        mock.Setup(h => h.HandleAsync(It.IsAny<TCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<IAuthorizationContextAccessor> CreateContextAccessor(
        bool hasPrincipal, IReadOnlyCollection<string> roles = null)
    {
        var mock = new Mock<IAuthorizationContextAccessor>();
        mock.Setup(a => a.Current).Returns(new AuthorizationContext(hasPrincipal, roles));
        return mock;
    }

    #endregion

    /// <summary>
    /// Property 24: REST error codes are compatible with DefaultRestRulesProvider —
    /// For any command or query response produced by the authorization decorator, the OutcomeEntry
    /// error codes (GenericErrorCodes.NotAuthorized and GenericErrorCodes.NotAuthenticated) SHALL be
    /// the same codes already handled by the existing DefaultRestRulesProvider, requiring no custom
    /// IRestRulesProvider.
    /// **Validates: Requirements 19.1, 19.2, 19.3**
    /// </summary>
    [TestMethod]
    public void Property24_RestErrorCodes_AreCompatibleWithDefaultRestRulesProvider()
    {
        var provider = new DefaultRestRulesProvider();
        var commandRules = provider.GetCommandRules().ToList();
        var queryRules = provider.GetQueryRules().ToList();

        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, isUnauthenticated =>
        {
            AuthorizationDescriptorCache.Clear();

            ICommandResponse deniedResponse;

            if (isUnauthenticated)
            {
                // Unauthenticated denial — should use GenericErrorCodes.NotAuthenticated
                var innerHandler = CreateMockHandler<RestTestProtectedCommand>();
                var contextAccessor = CreateContextAccessor(false);
                var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);
                deniedResponse = decorator.HandleAsync(new RestTestProtectedCommand(), CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            else
            {
                // Unauthorized denial — should use GenericErrorCodes.NotAuthorized
                var innerHandler = CreateMockHandler<RestTestProtectedCommand>();
                var contextAccessor = CreateContextAccessor(true, new List<string> { "Viewer" }.AsReadOnly());
                var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);
                deniedResponse = decorator.HandleAsync(new RestTestProtectedCommand(), CancellationToken.None)
                    .GetAwaiter().GetResult();
            }

            // The response must be unsuccessful with exactly one outcome entry
            if (deniedResponse.Successful || deniedResponse.OutcomeEntries.Count != 1)
                return false.ToProperty();

            var errorCode = deniedResponse.OutcomeEntries[0].ErrorCode;

            // Verify the error code is one of the two authorization-related codes
            var isKnownCode = errorCode == GenericErrorCodes.NotAuthorized
                           || errorCode == GenericErrorCodes.NotAuthenticated;

            // Verify at least one command rule in DefaultRestRulesProvider matches this response
            var matchedByCommandRule = commandRules.Any(rule =>
                rule.RuleCondition != null && rule.RuleCondition(deniedResponse));

            return (isKnownCode && matchedByCommandRule).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Verifies that GenericErrorCodes.NotAuthorized is handled by DefaultRestRulesProvider command rules.
    /// **Validates: Requirements 19.1**
    /// </summary>
    [TestMethod]
    public void Property24_NotAuthorizedErrorCode_IsHandledByDefaultRestRulesProvider()
    {
        AuthorizationDescriptorCache.Clear();
        var provider = new DefaultRestRulesProvider();
        var commandRules = provider.GetCommandRules().ToList();

        // Create a 403 denial response (authenticated but unauthorized)
        var innerHandler = CreateMockHandler<RestTestProtectedCommand>();
        var contextAccessor = CreateContextAccessor(true, new List<string> { "Viewer" }.AsReadOnly());
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);
        var response = decorator.HandleAsync(new RestTestProtectedCommand(), CancellationToken.None)
            .GetAwaiter().GetResult();

        response.OutcomeEntries[0].ErrorCode.Should().Be(GenericErrorCodes.NotAuthorized);
        commandRules.Any(rule => rule.RuleCondition != null && rule.RuleCondition(response)).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that GenericErrorCodes.NotAuthenticated is handled by DefaultRestRulesProvider command rules.
    /// **Validates: Requirements 19.2**
    /// </summary>
    [TestMethod]
    public void Property24_NotAuthenticatedErrorCode_IsHandledByDefaultRestRulesProvider()
    {
        AuthorizationDescriptorCache.Clear();
        var provider = new DefaultRestRulesProvider();
        var commandRules = provider.GetCommandRules().ToList();

        // Create a 401 denial response (unauthenticated)
        var innerHandler = CreateMockHandler<RestTestProtectedCommand>();
        var contextAccessor = CreateContextAccessor(false);
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);
        var response = decorator.HandleAsync(new RestTestProtectedCommand(), CancellationToken.None)
            .GetAwaiter().GetResult();

        response.OutcomeEntries[0].ErrorCode.Should().Be(GenericErrorCodes.NotAuthenticated);
        commandRules.Any(rule => rule.RuleCondition != null && rule.RuleCondition(response)).Should().BeTrue();
    }
}
