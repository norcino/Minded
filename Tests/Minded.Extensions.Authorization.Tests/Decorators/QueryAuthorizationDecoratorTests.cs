using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Authorization.Tests.Decorators;

#region Test Query Types — Envelope (IQueryResponse<T>)

public class UnprotectedEnvelopeQuery : IQuery<IQueryResponse<string>>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireRoles("Admin")]
public class ProtectedEnvelopeQuery : IQuery<IQueryResponse<string>>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequirePermissions("orders.read")]
public class PermissionProtectedEnvelopeQuery : IQuery<IQueryResponse<string>>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[AllowUnauthenticated]
public class AllowUnauthenticatedEnvelopeQuery : IQuery<IQueryResponse<string>>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireAuthentication]
public class RequireAuthOnlyEnvelopeQuery : IQuery<IQueryResponse<string>>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

#endregion

#region Test Query Types — Raw result

public class UnprotectedRawQuery : IQuery<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireRoles("Admin")]
public class ProtectedRawQuery : IQuery<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[AllowUnauthenticated]
public class AllowUnauthenticatedRawQuery : IQuery<int>
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

#endregion

[TestClass]
public class QueryAuthorizationDecoratorTests
{
    private static readonly string[] RolePool = { "Admin", "Manager", "Editor", "Viewer", "SuperAdmin" };
    private static readonly string[] PermissionPool = { "orders.read", "orders.write", "users.manage", "reports.view" };

    #region Helpers

    private static AuthorizationQueryHandlerDecorator<TQuery, TResult> CreateDecorator<TQuery, TResult>(
        IQueryHandler<TQuery, TResult> innerHandler,
        IAuthorizationContextAccessor contextAccessor,
        IRequestAuthorizationEvaluator evaluator = null,
        AuthorizationOptions options = null) where TQuery : IQuery<TResult>
    {
        evaluator ??= new DefaultRequestAuthorizationEvaluator();
        options ??= new AuthorizationOptions();
        var optionsWrapper = Options.Create(options);
        var logger = NullLoggerFactory.Instance.CreateLogger<AuthorizationQueryHandlerDecorator<TQuery, TResult>>();
        var mindedContextAccessor = new MindedContextAccessor();
        var mediator = new Mock<IMediator>().Object;

        return new AuthorizationQueryHandlerDecorator<TQuery, TResult>(
            innerHandler, contextAccessor, evaluator, optionsWrapper, mindedContextAccessor, mediator, logger);
    }

    private static Mock<IQueryHandler<TQuery, TResult>> CreateMockHandler<TQuery, TResult>(TResult response = default) where TQuery : IQuery<TResult>
    {
        var mock = new Mock<IQueryHandler<TQuery, TResult>>();
        mock.Setup(h => h.HandleAsync(It.IsAny<TQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<IAuthorizationContextAccessor> CreateContextAccessor(
        bool hasPrincipal,
        IReadOnlyCollection<string> roles = null,
        IReadOnlyCollection<string> permissions = null)
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
    /// For any protected query where the AuthorizationContext satisfies the descriptor,
    /// the decorator SHALL invoke the inner handler and return its response unmodified.
    /// **Validates: Requirements 10.1, 11.1**
    /// </summary>
    [TestMethod]
    public void Property11_AuthorizedRequests_PassThroughUnchanged()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = new QueryResponse<string>("test-result");
            var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(expectedResponse);
            var contextAccessor = CreateContextAccessor(true, new List<string>(roles) { "Admin" }.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 11 (raw): Authorized raw query requests pass through unchanged.
    /// **Validates: Requirements 11.1**
    /// </summary>
    [TestMethod]
    public void Property11_AuthorizedRawRequests_PassThroughUnchanged()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedRawQuery, int>(42);
            var contextAccessor = CreateContextAccessor(true, new List<string>(roles) { "Admin" }.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == 42).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 13: Denied queries with IQueryResponse return unsuccessful response with correct error code —
    /// For any protected query returning IQueryResponse&lt;T&gt; where the caller is authenticated but RBAC
    /// clauses are not satisfied, the decorator SHALL return an unsuccessful response with Successful = false
    /// and exactly one OutcomeEntry with ErrorCode = GenericErrorCodes.NotAuthorized and Severity.Error.
    /// **Validates: Requirements 11.2, 13.1, 13.4**
    /// </summary>
    [TestMethod]
    public void Property13_DeniedEnvelopeQueries_ReturnUnsuccessfulResponseWithCorrectErrorCode()
    {
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
                new QueryResponse<string>("should-not-reach"));
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthorized
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 14: Denied raw queries throw SecurityException —
    /// For any protected query returning a raw result type where the caller is authenticated but RBAC
    /// clauses are not satisfied, the decorator SHALL throw a System.Security.SecurityException.
    /// **Validates: Requirements 11.3**
    /// </summary>
    [TestMethod]
    public void Property14_DeniedRawQueries_ThrowSecurityException()
    {
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedRawQuery, int>(0);
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            try
            {
                decorator.HandleAsync(new ProtectedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();
                return false.ToProperty(); // Should have thrown
            }
            catch (SecurityException)
            {
                return true.ToProperty();
            }
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 15: Denied requests never invoke the inner handler —
    /// For any protected query where authorization is denied (either 401 or 403),
    /// the inner handler SHALL NOT be invoked.
    /// **Validates: Requirements 10.4, 11.4, 12.5**
    /// </summary>
    [TestMethod]
    public void Property15_DeniedEnvelopeRequests_NeverInvokeInnerHandler()
    {
        // Test 403 denial (authenticated but missing roles) on envelope query
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
                new QueryResponse<string>("should-not-reach"));
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            innerHandler.Verify(
                h => h.HandleAsync(It.IsAny<ProtectedEnvelopeQuery>(), It.IsAny<CancellationToken>()),
                Times.Never());
            return true.ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 15 (unauthenticated envelope): Unauthenticated denied envelope requests never invoke the inner handler.
    /// **Validates: Requirements 11.4, 12.5**
    /// </summary>
    [TestMethod]
    public void Property15_UnauthenticatedDeniedEnvelopeRequests_NeverInvokeInnerHandler()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
            new QueryResponse<string>("should-not-reach"));
        var contextAccessor = CreateContextAccessor(false);
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

        innerHandler.Verify(
            h => h.HandleAsync(It.IsAny<ProtectedEnvelopeQuery>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    /// <summary>
    /// Property 15 (raw denied): Denied raw query requests never invoke the inner handler.
    /// **Validates: Requirements 11.4, 12.5**
    /// </summary>
    [TestMethod]
    public void Property15_DeniedRawRequests_NeverInvokeInnerHandler()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<ProtectedRawQuery, int>(0);
        var contextAccessor = CreateContextAccessor(true, new List<string> { "Viewer" }.AsReadOnly());
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        try
        {
            decorator.HandleAsync(new ProtectedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (SecurityException) { }

        innerHandler.Verify(
            h => h.HandleAsync(It.IsAny<ProtectedRawQuery>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    /// <summary>
    /// Property 16: Unauthenticated callers on envelope responses get 401 —
    /// For any protected query returning IQueryResponse&lt;T&gt; where HasPrincipal is false,
    /// the decorator SHALL return an unsuccessful response with exactly one OutcomeEntry
    /// with ErrorCode = GenericErrorCodes.NotAuthenticated and Severity.Error.
    /// **Validates: Requirements 12.1, 12.2, 12.3, 12.6, 13.1, 13.4**
    /// </summary>
    [TestMethod]
    public void Property16_UnauthenticatedCallers_EnvelopeGet401()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
                new QueryResponse<string>("should-not-reach"));
            var contextAccessor = CreateContextAccessor(false, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthenticated
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 17: Unauthenticated callers on raw queries throw UnauthorizedAccessException —
    /// For any protected query returning a raw result type where HasPrincipal is false,
    /// the decorator SHALL throw a System.UnauthorizedAccessException.
    /// **Validates: Requirements 12.4, 12.6**
    /// </summary>
    [TestMethod]
    public void Property17_UnauthenticatedCallers_RawQueryThrowUnauthorizedAccessException()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<ProtectedRawQuery, int>(0);
            var contextAccessor = CreateContextAccessor(false, roles.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            try
            {
                decorator.HandleAsync(new ProtectedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();
                return false.ToProperty(); // Should have thrown
            }
            catch (UnauthorizedAccessException)
            {
                return true.ToProperty();
            }
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
            var rolesWithoutAdmin = contextRoles.Where(r => !r.Equals("Admin", StringComparison.OrdinalIgnoreCase)).ToList();
            if (rolesWithoutAdmin.Count == 0) rolesWithoutAdmin.Add("Viewer");

            var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
                new QueryResponse<string>("should-not-reach"));
            var contextAccessor = CreateContextAccessor(true, rolesWithoutAdmin.AsReadOnly());
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            var entry = result.OutcomeEntries[0];
            var message = entry.Message ?? string.Empty;

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
        var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
            new QueryResponse<string>("should-not-reach"));
        var contextAccessor = CreateContextAccessor(false);
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        var result = decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

        var entry = result.OutcomeEntries[0];
        var message = entry.Message ?? string.Empty;
        var allNames = RolePool.Concat(PermissionPool);
        allNames.Any(name => message.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).Should().BeFalse();
    }

    /// <summary>
    /// Property 19: Unprotected requests pass through without checks —
    /// For any query with no RBAC attributes and no enforce-authentication policy,
    /// the decorator SHALL invoke the inner handler without performing any authorization checks,
    /// regardless of the AuthorizationContext.
    /// **Validates: Requirements 10.5, 11.5, 25.4**
    /// </summary>
    [TestMethod]
    public void Property19_UnprotectedEnvelopeRequests_PassThroughWithoutChecks()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = new QueryResponse<string>("unprotected-result");
            var innerHandler = CreateMockHandler<UnprotectedEnvelopeQuery, IQueryResponse<string>>(expectedResponse);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new UnprotectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 19 (raw): Unprotected raw query requests pass through without checks.
    /// **Validates: Requirements 11.5, 25.4**
    /// </summary>
    [TestMethod]
    public void Property19_UnprotectedRawRequests_PassThroughWithoutChecks()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<UnprotectedRawQuery, int>(99);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

            var result = decorator.HandleAsync(new UnprotectedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == 99).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 20: Enforce-authentication policy denies unattributed unauthenticated requests —
    /// For any query (when RequireAuthenticationForAllQueries is enabled) that has no RBAC attributes
    /// and no AllowUnauthenticatedAttribute, the decorator SHALL deny unauthenticated callers
    /// with GenericErrorCodes.NotAuthenticated.
    /// **Validates: Requirements 25.1, 25.2**
    /// </summary>
    [TestMethod]
    public void Property20_EnforceAuthPolicy_DeniesUnattributedUnauthenticatedEnvelopeRequests()
    {
        var arb = Gen.Constant(true).ToArbitrary();

        Prop.ForAll(arb, _ =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<UnprotectedEnvelopeQuery, IQueryResponse<string>>(
                new QueryResponse<string>("should-not-reach"));
            var contextAccessor = CreateContextAccessor(false);
            var options = new AuthorizationOptions { RequireAuthenticationForAllQueries = true };
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

            var result = decorator.HandleAsync(new UnprotectedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result.Successful == false
                && result.OutcomeEntries.Count == 1
                && result.OutcomeEntries[0].ErrorCode == GenericErrorCodes.NotAuthenticated
                && result.OutcomeEntries[0].Severity == Severity.Error).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 20 (raw): Enforce-auth policy denies unattributed unauthenticated raw queries.
    /// **Validates: Requirements 25.2**
    /// </summary>
    [TestMethod]
    public void Property20_EnforceAuthPolicy_DeniesUnattributedUnauthenticatedRawRequests()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<UnprotectedRawQuery, int>(0);
        var contextAccessor = CreateContextAccessor(false);
        var options = new AuthorizationOptions { RequireAuthenticationForAllQueries = true };
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

        Action act = () => decorator.HandleAsync(new UnprotectedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Property 21: AllowUnauthenticatedAttribute bypasses all checks under enforce-auth —
    /// For any request carrying AllowUnauthenticatedAttribute when the enforce-authentication policy
    /// is enabled, the decorator SHALL pass through to the inner handler without performing any
    /// authentication or authorization checks.
    /// **Validates: Requirements 25.3**
    /// </summary>
    [TestMethod]
    public void Property21_AllowUnauthenticated_BypassesAllChecksUnderEnforceAuth_Envelope()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var expectedResponse = new QueryResponse<string>("allowed-result");
            var innerHandler = CreateMockHandler<AllowUnauthenticatedEnvelopeQuery, IQueryResponse<string>>(expectedResponse);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var options = new AuthorizationOptions { RequireAuthenticationForAllQueries = true };
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

            var result = decorator.HandleAsync(new AllowUnauthenticatedEnvelopeQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == expectedResponse).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 21 (raw): AllowUnauthenticated bypasses checks for raw queries under enforce-auth.
    /// **Validates: Requirements 25.3**
    /// </summary>
    [TestMethod]
    public void Property21_AllowUnauthenticated_BypassesAllChecksUnderEnforceAuth_Raw()
    {
        var arb = Gen.Elements(true, false).ToArbitrary();

        Prop.ForAll(arb, hasPrincipal =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<AllowUnauthenticatedRawQuery, int>(77);
            var contextAccessor = CreateContextAccessor(hasPrincipal);
            var options = new AuthorizationOptions { RequireAuthenticationForAllQueries = true };
            var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, options: options);

            var result = decorator.HandleAsync(new AllowUnauthenticatedRawQuery(), CancellationToken.None).GetAwaiter().GetResult();

            return (result == 77).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Exception scoping: an exception thrown by the inner handler of an authorized envelope query
    /// SHALL propagate to the caller (so the Exception decorator can handle it) instead of being
    /// converted into a NotAuthorized denial by the deny-by-default guard.
    /// </summary>
    [TestMethod]
    public async Task HandlerException_OnAuthorizedEnvelopeQuery_Propagates()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = new Mock<IQueryHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>>();
        innerHandler.Setup(h => h.HandleAsync(It.IsAny<ProtectedEnvelopeQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failure"));
        var contextAccessor = CreateContextAccessor(true, new List<string> { "Admin" }.AsReadOnly());
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        var act = () => decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failure");
    }

    /// <summary>
    /// Exception scoping (raw): inner-handler exceptions propagate unchanged for raw queries —
    /// they must not be remapped to SecurityException by the denial path.
    /// </summary>
    [TestMethod]
    public async Task HandlerException_OnAuthorizedRawQuery_Propagates()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = new Mock<IQueryHandler<ProtectedRawQuery, int>>();
        innerHandler.Setup(h => h.HandleAsync(It.IsAny<ProtectedRawQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failure"));
        var contextAccessor = CreateContextAccessor(true, new List<string> { "Admin" }.AsReadOnly());
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object);

        var act = () => decorator.HandleAsync(new ProtectedRawQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failure");
    }

    /// <summary>
    /// Deny-by-default: an exception thrown inside the authorization flow itself (here the evaluator)
    /// SHALL still result in a NotAuthorized envelope denial without invoking the inner handler.
    /// </summary>
    [TestMethod]
    public async Task AuthorizationFlowException_EnvelopeQuery_DeniesWithoutInvokingHandler()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<ProtectedEnvelopeQuery, IQueryResponse<string>>(
            new QueryResponse<string>("should-not-reach"));
        var contextAccessor = CreateContextAccessor(true, new List<string> { "Admin" }.AsReadOnly());
        var evaluator = new Mock<IRequestAuthorizationEvaluator>();
        evaluator.Setup(e => e.Evaluate(It.IsAny<Type>(), It.IsAny<AuthorizationDescriptor>(), It.IsAny<AuthorizationContext>()))
            .Throws(new InvalidOperationException("evaluator failure"));
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, evaluator.Object);

        var result = await decorator.HandleAsync(new ProtectedEnvelopeQuery(), CancellationToken.None);

        result.Successful.Should().BeFalse();
        result.OutcomeEntries.Should().HaveCount(1);
        result.OutcomeEntries[0].ErrorCode.Should().Be(GenericErrorCodes.NotAuthorized);
        innerHandler.Verify(h => h.HandleAsync(It.IsAny<ProtectedEnvelopeQuery>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    /// <summary>
    /// Deny-by-default (raw): authorization-flow exceptions on raw queries surface as
    /// SecurityException without invoking the inner handler.
    /// </summary>
    [TestMethod]
    public async Task AuthorizationFlowException_RawQuery_ThrowsSecurityExceptionWithoutInvokingHandler()
    {
        AuthorizationDescriptorCache.Clear();
        var innerHandler = CreateMockHandler<ProtectedRawQuery, int>(42);
        var contextAccessor = CreateContextAccessor(true, new List<string> { "Admin" }.AsReadOnly());
        var evaluator = new Mock<IRequestAuthorizationEvaluator>();
        evaluator.Setup(e => e.Evaluate(It.IsAny<Type>(), It.IsAny<AuthorizationDescriptor>(), It.IsAny<AuthorizationContext>()))
            .Throws(new InvalidOperationException("evaluator failure"));
        var decorator = CreateDecorator(innerHandler.Object, contextAccessor.Object, evaluator.Object);

        var act = () => decorator.HandleAsync(new ProtectedRawQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<SecurityException>();
        innerHandler.Verify(h => h.HandleAsync(It.IsAny<ProtectedRawQuery>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
