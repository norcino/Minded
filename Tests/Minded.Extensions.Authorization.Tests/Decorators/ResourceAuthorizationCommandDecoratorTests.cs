using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Authorization.Tests.Decorators;

#region Resource authorization test types

public class CanUserAccessProjectQuery : IQuery<bool>
{
    public CanUserAccessProjectQuery(object resourceId, string claimValue)
    {
        ResourceId = resourceId;
        ClaimValue = claimValue;
    }

    public object ResourceId { get; }
    public string ClaimValue { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireResourceAccess(nameof(ProjectId), "UserId", typeof(CanUserAccessProjectQuery))]
public class UpdateProjectCommand : ICommand
{
    public UpdateProjectCommand(Guid projectId)
    {
        ProjectId = projectId;
    }

    public Guid ProjectId { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireResourceAccess(nameof(ProjectId), "UserId", typeof(CanUserAccessProjectQuery), OrAnyRole = new[] { "Admin" })]
public class UpdateProjectWithBypassCommand : ICommand
{
    public UpdateProjectWithBypassCommand(Guid projectId)
    {
        ProjectId = projectId;
    }

    public Guid ProjectId { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

[RequireClaim("OrganizationId", MatchProperty = nameof(OrgId))]
public class UpdateOrgCommand : ICommand
{
    public UpdateOrgCommand(string orgId)
    {
        OrgId = orgId;
    }

    public string OrgId { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

#endregion

[TestClass]
public class ResourceAuthorizationCommandDecoratorTests
{
    [TestInitialize]
    public void Setup()
    {
        AuthorizationDescriptorCache.Clear();
    }

    internal static AuthorizationCommandHandlerDecorator<TCommand> CreateDecorator<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        AuthorizationContext context,
        IMediator mediator,
        IMindedContextAccessor mindedContextAccessor = null) where TCommand : ICommand
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor.Setup(a => a.Current).Returns(context);

        return new AuthorizationCommandHandlerDecorator<TCommand>(
            innerHandler,
            accessor.Object,
            new DefaultRequestAuthorizationEvaluator(),
            Options.Create(new AuthorizationOptions()),
            mindedContextAccessor ?? CreateAccessorWithContext(),
            mediator,
            NullLoggerFactory.Instance.CreateLogger<AuthorizationCommandHandlerDecorator<TCommand>>());
    }

    internal static IMindedContextAccessor CreateAccessorWithContext()
    {
        var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, default, null);
        var mock = new Mock<IMindedContextAccessor>();
        mock.Setup(a => a.Current).Returns(context);
        return mock.Object;
    }

    private static AuthorizationContext PrincipalWith(
        IDictionary<string, string> claims = null,
        IReadOnlyCollection<string> roles = null,
        IReadOnlyCollection<string> permissions = null)
        => new AuthorizationContext(true, roles, permissions,
            new Dictionary<string, string>(claims ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase));

    [TestMethod]
    public async Task Decorator_DispatchesAuthorizationQuery_AndAllowsWhenGranted()
    {
        var inner = new Mock<ICommandHandler<UpdateProjectCommand>>();
        inner.Setup(h => h.HandleAsync(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(CommandResponse.Success());

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var context = PrincipalWith(new Dictionary<string, string> { ["UserId"] = "u-1" });
        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateProjectCommand(Guid.NewGuid()));

        response.Successful.Should().BeTrue();
        mediator.Verify(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()), Times.Once);
        inner.Verify(h => h.HandleAsync(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Decorator_DeniesWithUnauthorized_WhenAuthorizationQueryReturnsFalse()
    {
        var inner = new Mock<ICommandHandler<UpdateProjectCommand>>();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var context = PrincipalWith(new Dictionary<string, string> { ["UserId"] = "u-1" });
        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateProjectCommand(Guid.NewGuid()));

        response.Successful.Should().BeFalse();
        response.OutcomeEntries.Should().ContainSingle().Which.ErrorCode.Should().Be(GenericErrorCodes.NotAuthorized);
        inner.Verify(h => h.HandleAsync(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Decorator_DeniesWhenClaimMissing_WithoutDispatchingQuery()
    {
        var inner = new Mock<ICommandHandler<UpdateProjectCommand>>();
        var mediator = new Mock<IMediator>();
        var context = PrincipalWith();

        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateProjectCommand(Guid.NewGuid()));

        response.Successful.Should().BeFalse();
        response.OutcomeEntries.Should().ContainSingle().Which.ErrorCode.Should().Be(GenericErrorCodes.NotAuthorized);
        mediator.Verify(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Decorator_OrAnyRole_ShortCircuitsResourceQuery()
    {
        var inner = new Mock<ICommandHandler<UpdateProjectWithBypassCommand>>();
        inner.Setup(h => h.HandleAsync(It.IsAny<UpdateProjectWithBypassCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(CommandResponse.Success());

        var mediator = new Mock<IMediator>();
        var context = PrincipalWith(roles: new[] { "Admin" });

        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateProjectWithBypassCommand(Guid.NewGuid()));

        response.Successful.Should().BeTrue();
        mediator.Verify(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Decorator_MatchPropertyClaim_AllowsWhenValueMatches()
    {
        var inner = new Mock<ICommandHandler<UpdateOrgCommand>>();
        inner.Setup(h => h.HandleAsync(It.IsAny<UpdateOrgCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(CommandResponse.Success());
        var mediator = new Mock<IMediator>();
        var context = PrincipalWith(new Dictionary<string, string> { ["OrganizationId"] = "Org-1" });

        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateOrgCommand("org-1"));

        response.Successful.Should().BeTrue();
    }

    [TestMethod]
    public async Task Decorator_MatchPropertyClaim_DeniesWhenValueDiffers()
    {
        var inner = new Mock<ICommandHandler<UpdateOrgCommand>>();
        var mediator = new Mock<IMediator>();
        var context = PrincipalWith(new Dictionary<string, string> { ["OrganizationId"] = "Org-2" });

        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateOrgCommand("org-1"));

        response.Successful.Should().BeFalse();
        response.OutcomeEntries.Should().ContainSingle().Which.ErrorCode.Should().Be(GenericErrorCodes.NotAuthorized);
        inner.Verify(h => h.HandleAsync(It.IsAny<UpdateOrgCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
