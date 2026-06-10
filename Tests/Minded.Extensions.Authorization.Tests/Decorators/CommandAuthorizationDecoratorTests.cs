using System;
using System.Collections.Generic;
using System.Linq;
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
using Minded.Extensions.Context;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Authorization.Tests.Decorators;

#region Test Command Types

public class UnprotectedCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireRoles("Admin")]
public class ProtectedCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireRoles("Admin", "Manager")]
public class MultiRoleCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequirePermissions("orders.write")]
public class PermissionProtectedCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[AllowUnauthenticated]
public class AllowUnauthenticatedCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireAuthentication]
public class RequireAuthOnlyCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

// Command with result variants
public class UnprotectedCommandWithResult : ICommand<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireRoles("Admin")]
public class ProtectedCommandWithResult : ICommand<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[AllowUnauthenticated]
public class AllowUnauthenticatedCommandWithResult : ICommand<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireAuthentication]
public class RequireAuthOnlyCommandWithResult : ICommand<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

#endregion

[TestClass]
public class CommandAuthorizationDecoratorTests
{
    private static readonly string[] RolePool = { "Admin", "Manager", "Editor", "Viewer", "SuperAdmin" };
    private static readonly string[] PermissionPool = { "orders.read", "orders.write", "users.manage", "reports.view" };

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
        var mindedContextAccessor = new MindedContextAccessor();
        var mediator = new Mock<IMediator>().Object;

        return new AuthorizationCommandHandlerDecorator<TCommand>(
            innerHandler, contextAccessor, evaluator, optionsWrapper, mindedContextAccessor, mediator, logger);
    }

    private static AuthorizationCommandHandlerDecorator<TCommand, TResult> CreateDecoratorWithResult<TCommand, TResult>(
        ICommandHandler<TCommand, TResult> innerHandler,
        IAuthorizationContextAccessor contextAccessor,
        IRequestAuthorizationEvaluator evaluator = null,
        AuthorizationOptions options = null) where TCommand : ICommand<TResult>
    {
        evaluator ??= new DefaultRequestAuthorizationEvaluator();
        options ??= new AuthorizationOptions();
        var optionsWrapper = Options.Create(options);
        var logger = NullLoggerFactory.Instance.CreateLogger<AuthorizationCommandHandlerDecorator<TCommand, TResult>>();
        var mindedContextAccessor = new MindedContextAccessor();
        var mediator = new Mock<IMediator>().Object;

        return new AuthorizationCommandHandlerDecorator<TCommand, TResult>(
            innerHandler, contextAccessor, evaluator, optionsWrapper, mindedContextAccessor, mediator, logger);
    }

    private static Mock<ICommandHandler<TCommand>> CreateMockHandler<TCommand>(ICommandResponse response = null) where TCommand : ICommand
    {
        var mock = new Mock<ICommandHandler<TCommand>>();
        response ??= CommandResponse.Success();
        mock.Setup(h => h.HandleAsync(It.IsAny<TCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<ICommandHandler<TCommand, TResult>> CreateMockHandlerWithResult<TCommand, TResult>(
        ICommandResponse<TResult> response = null) where TCommand : ICommand<TResult>
    {
        var mock = new Mock<ICommandHandler<TCommand, TResult>>();
        mock.Setup(h => h.HandleAsync(It.IsAny<TCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<IAuthorizationContextAccessor> CreateContextAccessor(bool hasPrincipal, IReadOnlyCollection<string> roles = null, IReadOnlyCollection<string> permissions = null)
    {
        var mock = new Mock<IAuthorizationContextAccessor>();
        mock.Setup(a => a.Current).Returns(new AuthorizationContext(hasPrincipal, roles, permissions));
        return mock;
    }

    private static Gen<List<string>> SubsetGen(string[] pool)
    {
        return Gen.SubListOf(pool).Select(l => l.ToList()).Where(l => l.Count > 0);
    }

    #endregion

    /// <summary>
    /// Property 11: Authorized requests pass through unchanged —
    /// For any protected command where the AuthorizationContext satisfies the descriptor,
    /// the decorator SHALL invoke the inner handler and return its response unmodified.
    /// **Validates: Requirements 10.1, 11.1**
    /// </summary>
    [TestMethod]
    public void Property11_AuthorizedRequests_PassThroughUnchanged()
    {
        // Test with ICommand (no result)
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse.Success();
            var innerHandler = CreateMockHandler<ProtectedCommand>(expectedResponse);
            // ProtectedCommand requires "Admin" role
            var contextAccessor = CreateContextAccessor(true, new List<string>(roles) { "Admin" }.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 11 (with result): Authorized requests pass through unchanged for ICommand{TResult}.
    /// **Validates: Requirements 10.1, 11.1**
    /// </summary>
    [TestMethod]
    public void Property11_AuthorizedRequestsWithResult_PassThroughUnchanged()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse<int>.Success(42);
            var innerHandler = CreateMockHandlerWithResult<ProtectedCommandWithResult, int>(expectedResponse);
            var contextAccessor = CreateContextAccessor(true, new List<string>(roles) { "Admin" }.AsReadOnly());
            var decorator = CreateDecoratorWithResult<ProtectedCommandWithResult, int>(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommandWithResult(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 12: Denied commands return unsuccessful response with correct error code —
    /// For any protected command where the caller is authenticated but RBAC clauses are not satisfied,
    /// the decorator SHALL return an unsuccessful response with Successful = false and exactly one
    /// OutcomeEntry with ErrorCode = GenericErrorCodes.NotAuthorized and Severity.Error.
    /// **Validates: Requirements 10.2, 10.3, 12.7, 13.1, 13.4**
    /// </summary>
    [TestMethod]
    public void Property12_DeniedCommands_ReturnUnsuccessfulResponseWithCorrectErrorCode()
    {
        // Test ICommand (no result)
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedCommand>();
            // ProtectedCommand requires "Admin" — provide roles that don't include Admin
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthorized
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 12 (with result): Denied commands with TResult return unsuccessful response.
    /// **Validates: Requirements 10.3, 12.7, 13.1, 13.4**
    /// </summary>
    [TestMethod]
    public void Property12_DeniedCommandsWithResult_ReturnUnsuccessfulResponseWithCorrectErrorCode()
    {
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandlerWithResult<ProtectedCommandWithResult, int>(CommandResponse<int>.Success(42));
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecoratorWithResult<ProtectedCommandWithResult, int>(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommandWithResult(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthorized
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 15: Denied requests never invoke the inner handler —
    /// For any protected command where authorization is denied (either 401 or 403),
    /// the inner handler SHALL NOT be invoked.
    /// **Validates: Requirements 10.4, 11.4, 12.5**
    /// </summary>
    [TestMethod]
    public void Property15_DeniedRequests_NeverInvokeInnerHandler()
    {
        // Test 403 denial (authenticated but missing roles)
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedCommand>();
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            innerHandler.Verify(h => h.HandleAsync(It.IsAny<ProtectedCommand>(), It.IsAny<CancellationToken>()), Times.Never());
            return true.ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 15 (unauthenticated): Unauthenticated denied requests never invoke the inner handler.
    /// **Validates: Requirements 10.4, 12.5**
    /// </summary>
    [TestMethod]
    public void Property15_UnauthenticatedDeniedRequests_NeverInvokeInnerHandler()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<ProtectedCommand>();
        var contextAccessor = CreateContextAccessor(false);
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

        innerHandler.Verify(h => h.HandleAsync(It.IsAny<ProtectedCommand>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    /// <summary>
    /// Property 16: Unauthenticated callers on envelope responses get 401 —
    /// For any protected command where HasPrincipal is false, the decorator SHALL return
    /// an unsuccessful response with exactly one OutcomeEntry with ErrorCode = GenericErrorCodes.NotAuthenticated
    /// and Severity.Error, without evaluating any RBAC clauses.
    /// **Validates: Requirements 12.1, 12.2, 12.3, 12.6, 13.1, 13.4**
    /// </summary>
    [TestMethod]
    public void Property16_UnauthenticatedCallers_GetNotAuthenticatedErrorCode()
    {
        // Test with ICommand
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedCommand>();
            var contextAccessor = CreateContextAccessor(false, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthenticated
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 16 (with result): Unauthenticated callers on ICommandResponse{TResult} get 401.
    /// **Validates: Requirements 12.1, 12.2, 12.6, 13.1, 13.4**
    /// </summary>
    [TestMethod]
    public void Property16_UnauthenticatedCallersWithResult_GetNotAuthenticatedErrorCode()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandlerWithResult<ProtectedCommandWithResult, int>(CommandResponse<int>.Success(42));
            var contextAccessor = CreateContextAccessor(false, roles.AsReadOnly());
            var decorator = CreateDecoratorWithResult<ProtectedCommandWithResult, int>(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommandWithResult(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthenticated
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 18: Denial OutcomeEntry contains no detail leakage —
    /// For any authorization denial, the OutcomeEntry message SHALL NOT contain any of the
    /// specific role names or permission names from the descriptor's clauses or the caller's context.
    /// **Validates: Requirements 13.2, 13.3**
    /// </summary>
    [TestMethod]
    public void Property18_DenialOutcomeEntry_ContainsNoDetailLeakage()
    {
        var arb = (from contextRoles in SubsetGen(RolePool)
                   select contextRoles).ToArbitrary();

        Prop.ForAll(arb, contextRoles =>
        {
            AuthorizationDescriptorCache.Clear();
            // ProtectedCommand requires "Admin" — test with roles that don't include Admin
            var rolesWithoutAdmin = contextRoles.Where(r => !r.Equals("Admin", StringComparison.OrdinalIgnoreCase)).ToList();
            if (rolesWithoutAdmin.Count == 0) rolesWithoutAdmin.Add("Viewer");

            var innerHandler = CreateMockHandler<ProtectedCommand>();
            var contextAccessor = CreateContextAccessor(true, rolesWithoutAdmin.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            var entry = result.OutcomeEntries[0];
            var message = entry.Message ?? string.Empty;

            // Message must not contain any role or permission names
            var allNames = RolePool.Concat(PermissionPool).Concat(rolesWithoutAdmin);
            var leaks = allNames.Any(name => message.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            return (!leaks).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 18 (unauthenticated): Unauthenticated denial OutcomeEntry contains no detail leakage.
    /// **Validates: Requirements 13.2, 13.3**
    /// </summary>
    [TestMethod]
    public void Property18_UnauthenticatedDenialOutcomeEntry_ContainsNoDetailLeakage()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<ProtectedCommand>();
        var contextAccessor = CreateContextAccessor(false);
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        var result = decorator.HandleAsync(new ProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

        var entry = result.OutcomeEntries[0];
        var message = entry.Message ?? string.Empty;
        var allNames = RolePool.Concat(PermissionPool);
        allNames.Any(name => message.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).Should().BeFalse();
    }

    /// <summary>
    /// Property 19: Unprotected requests pass through without checks —
    /// For any command with no RBAC attributes and no enforce-authentication policy,
    /// the decorator SHALL invoke the inner handler without performing any authorization checks,
    /// regardless of the AuthorizationContext.
    /// **Validates: Requirements 10.5, 11.5, 25.4**
    /// </summary>
    [TestMethod]
    public void Property19_UnprotectedRequests_PassThroughWithoutChecks()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse.Success();
            var innerHandler = CreateMockHandler<UnprotectedCommand>(expectedResponse);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new UnprotectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 19 (with result): Unprotected requests with result pass through without checks.
    /// **Validates: Requirements 10.5, 25.4**
    /// </summary>
    [TestMethod]
    public void Property19_UnprotectedRequestsWithResult_PassThroughWithoutChecks()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse<int>.Success(99);
            var innerHandler = CreateMockHandlerWithResult<UnprotectedCommandWithResult, int>(expectedResponse);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var decorator = CreateDecoratorWithResult<UnprotectedCommandWithResult, int>(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new UnprotectedCommandWithResult(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 20: Enforce-authentication policy denies unattributed unauthenticated requests —
    /// For any command (when RequireAuthenticationForAllCommands is enabled) that has no RBAC attributes
    /// and no AllowUnauthenticatedAttribute, the decorator SHALL deny unauthenticated callers
    /// with GenericErrorCodes.NotAuthenticated.
    /// **Validates: Requirements 25.1, 25.2**
    /// </summary>
    [TestMethod]
    public void Property20_EnforceAuthPolicy_DeniesUnattributedUnauthenticatedRequests()
    {
        var arb = Gen.Constant(true).ToArbitrary();

        Prop.ForAll(arb, _ =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<UnprotectedCommand>();
            var contextAccessor = CreateContextAccessor(false);
            var options = new AuthorizationOptions { RequireAuthenticationForAllCommands = true };
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

            var result = decorator.HandleAsync(new UnprotectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthenticated
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 20 (with provider): Enforce-auth via provider also denies unauthenticated.
    /// **Validates: Requirements 25.1, 25.2**
    /// </summary>
    [TestMethod]
    public void Property20_EnforceAuthPolicyViaProvider_DeniesUnattributedUnauthenticatedRequests()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<UnprotectedCommand>();
        var contextAccessor = CreateContextAccessor(false);
        var options = new AuthorizationOptions
        {
            RequireAuthenticationForAllCommandsProvider = () => true
        };
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

        var result = decorator.HandleAsync(new UnprotectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

        result.Successful.Should().BeFalse();
        result.OutcomeEntries.Should().HaveCount(1);
        result.OutcomeEntries[0].ErrorCode.Should().Be(GenericErrorCodes.NotAuthenticated);
    }

    /// <summary>
    /// Property 21: AllowUnauthenticatedAttribute bypasses all checks under enforce-auth —
    /// For any request carrying AllowUnauthenticatedAttribute when the enforce-authentication policy
    /// is enabled, the decorator SHALL pass through to the inner handler without performing any
    /// authentication or authorization checks.
    /// **Validates: Requirements 25.3**
    /// </summary>
    [TestMethod]
    public void Property21_AllowUnauthenticated_BypassesAllChecksUnderEnforceAuth()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse.Success();
            var innerHandler = CreateMockHandler<AllowUnauthenticatedCommand>(expectedResponse);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var options = new AuthorizationOptions { RequireAuthenticationForAllCommands = true };
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

            var result = decorator.HandleAsync(new AllowUnauthenticatedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 21 (with result): AllowUnauthenticated bypasses checks for ICommand{TResult} under enforce-auth.
    /// **Validates: Requirements 25.3**
    /// </summary>
    [TestMethod]
    public void Property21_AllowUnauthenticatedWithResult_BypassesAllChecksUnderEnforceAuth()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse<int>.Success(77);
            var innerHandler = CreateMockHandlerWithResult<AllowUnauthenticatedCommandWithResult, int>(expectedResponse);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var options = new AuthorizationOptions { RequireAuthenticationForAllCommands = true };
            var decorator = CreateDecoratorWithResult<AllowUnauthenticatedCommandWithResult, int>(innerHandler.Object, contextAccessor.Object, options: options);

            var result = decorator.HandleAsync(new AllowUnauthenticatedCommandWithResult(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 22: RequireAuthenticationAttribute with principal passes through without RBAC —
    /// For any request carrying RequireAuthenticationAttribute (without RBAC attributes) where
    /// HasPrincipal is true, the decorator SHALL invoke the inner handler without evaluating any RBAC clauses.
    /// **Validates: Requirements 26.7**
    /// </summary>
    [TestMethod]
    public void Property22_RequireAuthenticationWithPrincipal_PassesThroughWithoutRbac()
    {
        // Test with varying roles — should always pass through since no RBAC clauses
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse.Success();
            var innerHandler = CreateMockHandler<RequireAuthOnlyCommand>(expectedResponse);
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new RequireAuthOnlyCommand(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 22 (with result): RequireAuthenticationAttribute with principal passes through for ICommand{TResult}.
    /// **Validates: Requirements 26.7**
    /// </summary>
    [TestMethod]
    public void Property22_RequireAuthenticationWithPrincipalAndResult_PassesThroughWithoutRbac()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = CommandResponse<int>.Success(55);
            var innerHandler = CreateMockHandlerWithResult<RequireAuthOnlyCommandWithResult, int>(expectedResponse);
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecoratorWithResult<RequireAuthOnlyCommandWithResult, int>(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new RequireAuthOnlyCommandWithResult(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }
}
