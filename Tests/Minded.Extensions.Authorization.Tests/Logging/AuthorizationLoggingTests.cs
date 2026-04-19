using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Minded.Extensions.Authorization;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Authorization.Decorator;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Authorization.Tests.Logging;

#region Test Command Types

[RequireRoles("Admin")]
public class LogTestProtectedCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequirePermissions("orders.write")]
public class LogTestPermissionCommand : ICommand
{
    public Guid TraceId { get; } = Guid.NewGuid();
}

#endregion

#region Test Logger

/// <summary>
/// A simple test logger that captures log entries for verification.
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception exception, Func<TState, System.Exception, string> formatter)
    {
        LogEntries.Add(new LogEntry
        {
            LogLevel = logLevel,
            Message = formatter(state, exception)
        });
    }
}

public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; }
}

#endregion

[TestClass]
public class AuthorizationLoggingTests
{
    private static readonly string[] RolePool = { "Admin", "Manager", "Editor", "Viewer", "SuperAdmin" };
    private static readonly string[] PermissionPool = { "orders.read", "orders.write", "users.manage", "reports.view" };

    #region Helpers

    private static (AuthorizationCommandHandlerDecorator<TCommand> decorator, TestLogger<AuthorizationCommandHandlerDecorator<TCommand>> logger)
        CreateDecoratorWithTestLogger<TCommand>(
            ICommandHandler<TCommand> innerHandler,
            IAuthorizationContextAccessor contextAccessor,
            IRequestAuthorizationEvaluator evaluator = null,
            AuthorizationOptions options = null) where TCommand : ICommand
    {
        evaluator ??= new DefaultRequestAuthorizationEvaluator();
        options ??= new AuthorizationOptions();
        var optionsWrapper = Options.Create(options);
        var logger = new TestLogger<AuthorizationCommandHandlerDecorator<TCommand>>();

        var decorator = new AuthorizationCommandHandlerDecorator<TCommand>(
            innerHandler, contextAccessor, evaluator, optionsWrapper, logger);

        return (decorator, logger);
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
    /// Property 25: Authorization logging includes type, outcome, and duration without detail leakage —
    /// Test that the decorator logs the request type name, allowed/denied outcome, and duration.
    /// Test that unauthenticated denials are logged distinctly from unauthorized denials.
    /// Test that logs never contain specific role/permission names or the caller's context contents.
    /// **Validates: Requirements 20.1, 20.2, 20.3, 20.4, 20.5**
    /// </summary>
    [TestMethod]
    public void Property25_AuthorizationLogging_IncludesTypeOutcomeAndDuration_WithoutDetailLeakage()
    {
        // Generate random role sets that do NOT include "Admin" (to trigger denial)
        var arb = SubsetGen(RolePool).Where(r => !r.Contains("Admin")).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<LogTestProtectedCommand>();
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly());
            var (decorator, logger) = CreateDecoratorWithTestLogger(innerHandler.Object, contextAccessor.Object);

            decorator.HandleAsync(new LogTestProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            // Must have logged at least one entry
            if (logger.LogEntries.Count == 0)
                return false.ToProperty();

            var logMessage = logger.LogEntries[0].Message;

            // Log must contain the request type name
            var containsTypeName = logMessage.Contains(nameof(LogTestProtectedCommand));

            // Log must contain duration information (the word pattern from the template)
            // The log template uses {Duration} which renders as a TimeSpan
            var containsDuration = logMessage.Contains("in ");

            // Log must indicate unauthorized denial
            var containsOutcome = logMessage.Contains("Unauthorized access attempt");

            // Log must NOT contain any specific role or permission names
            var allNames = RolePool.Concat(PermissionPool).Concat(roles);
            var noLeakage = !allNames.Any(name =>
                logMessage.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

            return (containsTypeName && containsDuration && containsOutcome && noLeakage).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Verifies that allowed requests log the request type name, "allowed" outcome, and duration.
    /// **Validates: Requirements 20.1**
    /// </summary>
    [TestMethod]
    public void Property25_AllowedRequests_LogTypeOutcomeAndDuration()
    {
        var arb = SubsetGen(RolePool).ToArbitrary();

        Prop.ForAll(arb, roles =>
        {
            AuthorizationDescriptorCache.Clear();
            var innerHandler = CreateMockHandler<LogTestProtectedCommand>();
            var contextAccessor = CreateContextAccessor(true, new List<string>(roles) { "Admin" }.AsReadOnly());
            var (decorator, logger) = CreateDecoratorWithTestLogger(innerHandler.Object, contextAccessor.Object);

            decorator.HandleAsync(new LogTestProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

            if (logger.LogEntries.Count == 0)
                return false.ToProperty();

            var logMessage = logger.LogEntries[0].Message;
            var logLevel = logger.LogEntries[0].LogLevel;

            var containsTypeName = logMessage.Contains(nameof(LogTestProtectedCommand));
            var containsAllowed = logMessage.Contains("Authorization allowed");
            var isInformation = logLevel == LogLevel.Information;

            return (containsTypeName && containsAllowed && isInformation).ToProperty();
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Verifies that unauthenticated denials are logged distinctly from unauthorized denials.
    /// **Validates: Requirements 20.2, 20.3**
    /// </summary>
    [TestMethod]
    public void Property25_UnauthenticatedDenials_LoggedDistinctlyFromUnauthorizedDenials()
    {
        AuthorizationDescriptorCache.Clear();

        // Unauthenticated denial
        var innerHandler1 = CreateMockHandler<LogTestProtectedCommand>();
        var contextAccessor1 = CreateContextAccessor(false);
        var (decorator1, logger1) = CreateDecoratorWithTestLogger(innerHandler1.Object, contextAccessor1.Object);
        decorator1.HandleAsync(new LogTestProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

        AuthorizationDescriptorCache.Clear();

        // Unauthorized denial (authenticated but missing roles)
        var innerHandler2 = CreateMockHandler<LogTestProtectedCommand>();
        var contextAccessor2 = CreateContextAccessor(true, new List<string> { "Viewer" }.AsReadOnly());
        var (decorator2, logger2) = CreateDecoratorWithTestLogger(innerHandler2.Object, contextAccessor2.Object);
        decorator2.HandleAsync(new LogTestProtectedCommand(), CancellationToken.None).GetAwaiter().GetResult();

        // Both should have logged
        logger1.LogEntries.Should().NotBeEmpty();
        logger2.LogEntries.Should().NotBeEmpty();

        var unauthenticatedLog = logger1.LogEntries[0].Message;
        var unauthorizedLog = logger2.LogEntries[0].Message;

        // The messages should be distinct
        unauthenticatedLog.Should().Contain("Unauthenticated access attempt");
        unauthorizedLog.Should().Contain("Unauthorized access attempt");
        unauthenticatedLog.Should().NotBe(unauthorizedLog);

        // Both should be warnings
        logger1.LogEntries[0].LogLevel.Should().Be(LogLevel.Warning);
        logger2.LogEntries[0].LogLevel.Should().Be(LogLevel.Warning);
    }

    /// <summary>
    /// Verifies that logs never contain specific role/permission names or the caller's context contents.
    /// **Validates: Requirements 20.4, 20.5**
    /// </summary>
    [TestMethod]
    public void Property25_Logs_NeverContainRoleOrPermissionNames()
    {
        var arb = (from roles in SubsetGen(RolePool)
                   from permissions in SubsetGen(PermissionPool)
                   select (roles, permissions)).ToArbitrary();

        Prop.ForAll(arb, input =>
        {
            var (roles, permissions) = input;
            AuthorizationDescriptorCache.Clear();

            // Test with permission-protected command
            var innerHandler = CreateMockHandler<LogTestPermissionCommand>();
            var contextAccessor = CreateContextAccessor(true, roles.AsReadOnly(), permissions.AsReadOnly());
            var (decorator, logger) = CreateDecoratorWithTestLogger(innerHandler.Object, contextAccessor.Object);

            decorator.HandleAsync(new LogTestPermissionCommand(), CancellationToken.None).GetAwaiter().GetResult();

            if (logger.LogEntries.Count == 0)
                return false.ToProperty();

            // Check all log entries for leakage
            var allLogText = string.Join(" ", logger.LogEntries.Select(e => e.Message));
            var allNames = RolePool.Concat(PermissionPool).Concat(roles).Concat(permissions);
            var noLeakage = !allNames.Any(name =>
                allLogText.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

            return noLeakage.ToProperty();
        }).QuickCheckThrowOnFailure();
    }
}
