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
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Authorization.Decorator;
using Minded.Extensions.Context;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Authorization.Tests.Decorators;

[TestClass]
public class ResourceAuthorizationContextRequiredTests
{
    [TestInitialize]
    public void Setup() => AuthorizationDescriptorCache.Clear();

    private static AuthorizationCommandHandlerDecorator<TCommand> CreateDecoratorWithoutContext<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        AuthorizationContext context,
        IMediator mediator) where TCommand : ICommand
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor.Setup(a => a.Current).Returns(context);

        return new AuthorizationCommandHandlerDecorator<TCommand>(
            innerHandler,
            accessor.Object,
            new DefaultRequestAuthorizationEvaluator(),
            Options.Create(new AuthorizationOptions()),
            new MindedContextAccessor(),
            mediator,
            NullLoggerFactory.Instance.CreateLogger<AuthorizationCommandHandlerDecorator<TCommand>>());
    }

    [TestMethod]
    public async Task RequireResourceAccess_WithoutMindedContext_ThrowsMindedContextRequiredException()
    {
        var inner = new Mock<ICommandHandler<UpdateProjectCommand>>();
        var mediator = new Mock<IMediator>();

        var context = new AuthorizationContext(true, claims: new Dictionary<string, string>
        {
            ["UserId"] = "u-1"
        });

        var decorator = CreateDecoratorWithoutContext(inner.Object, context, mediator.Object);

        var act = () => decorator.HandleAsync(new UpdateProjectCommand(Guid.NewGuid()));

        var ex = await act.Should().ThrowExactlyAsync<MindedContextRequiredException>();
        ex.Which.Message.Should().Contain(nameof(UpdateProjectCommand));
        ex.Which.Message.Should().Contain("RequireResourceAccess");

        inner.Verify(h => h.HandleAsync(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        mediator.Verify(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task RequireResourceAccess_OrShortCircuit_DoesNotRequireMindedContext()
    {
        // OrAnyRole bypass means the resource clause never runs, so the missing context guard must not trigger.
        var inner = new Mock<ICommandHandler<UpdateProjectWithBypassCommand>>();
        inner.Setup(h => h.HandleAsync(It.IsAny<UpdateProjectWithBypassCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(CommandResponse.Success());

        var mediator = new Mock<IMediator>();

        var context = new AuthorizationContext(true, roles: new[] { "Admin" });

        var decorator = CreateDecoratorWithoutContext(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateProjectWithBypassCommand(Guid.NewGuid()));

        response.Successful.Should().BeTrue();
    }

    [TestMethod]
    public async Task RbacOnlyCommand_WithoutMindedContext_DoesNotThrow()
    {
        // No RequireResourceAccess attribute -> no MindedContext required.
        var inner = new Mock<ICommandHandler<UpdateOrgCommand>>();
        inner.Setup(h => h.HandleAsync(It.IsAny<UpdateOrgCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(CommandResponse.Success());

        var mediator = new Mock<IMediator>();
        var context = new AuthorizationContext(true, claims: new Dictionary<string, string>
        {
            ["OrganizationId"] = "Org-1"
        });

        var decorator = CreateDecoratorWithoutContext(inner.Object, context, mediator.Object);

        var response = await decorator.HandleAsync(new UpdateOrgCommand("org-1"));

        response.Successful.Should().BeTrue();
    }
}
