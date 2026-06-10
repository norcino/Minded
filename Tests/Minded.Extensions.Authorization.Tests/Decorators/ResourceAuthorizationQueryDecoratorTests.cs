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
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Authorization.Tests.Decorators;

[RequireResourceAccess(nameof(GroupId), "UserId", typeof(CanUserAccessProjectQuery))]
public class GetGroupQuery : IQuery<IQueryResponse<string>>
{
    public GetGroupQuery(Guid groupId)
    {
        GroupId = groupId;
    }

    public Guid GroupId { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

[TestClass]
public class ResourceAuthorizationQueryDecoratorTests
{
    [TestInitialize]
    public void Setup()
    {
        AuthorizationDescriptorCache.Clear();
    }

    private static AuthorizationQueryHandlerDecorator<TQuery, TResult> CreateDecorator<TQuery, TResult>(
        IQueryHandler<TQuery, TResult> innerHandler,
        AuthorizationContext context,
        IMediator mediator,
        IMindedContextAccessor mindedContextAccessor = null) where TQuery : IQuery<TResult>
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor.Setup(a => a.Current).Returns(context);

        return new AuthorizationQueryHandlerDecorator<TQuery, TResult>(
            innerHandler,
            accessor.Object,
            new DefaultRequestAuthorizationEvaluator(),
            Options.Create(new AuthorizationOptions()),
            mindedContextAccessor ?? ResourceAuthorizationCommandDecoratorTests.CreateAccessorWithContext(),
            mediator,
            NullLoggerFactory.Instance.CreateLogger<AuthorizationQueryHandlerDecorator<TQuery, TResult>>());
    }

    [TestMethod]
    public async Task QueryDecorator_DispatchesAuthorizationQuery_AndAllows()
    {
        var inner = new Mock<IQueryHandler<GetGroupQuery, IQueryResponse<string>>>();
        inner.Setup(h => h.HandleAsync(It.IsAny<GetGroupQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new QueryResponse<string>("ok"));

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var context = new AuthorizationContext(true, claims: new Dictionary<string, string>
        {
            ["UserId"] = "u-1"
        });

        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var result = await decorator.HandleAsync(new GetGroupQuery(Guid.NewGuid()));

        result.Successful.Should().BeTrue();
        result.Result.Should().Be("ok");
        mediator.Verify(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task QueryDecorator_DeniesWhenAuthorizationQueryReturnsFalse()
    {
        var inner = new Mock<IQueryHandler<GetGroupQuery, IQueryResponse<string>>>();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.ProcessQueryAsync(It.IsAny<IQuery<bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var context = new AuthorizationContext(true, claims: new Dictionary<string, string>
        {
            ["UserId"] = "u-1"
        });

        var decorator = CreateDecorator(inner.Object, context, mediator.Object);

        var result = await decorator.HandleAsync(new GetGroupQuery(Guid.NewGuid()));

        result.Successful.Should().BeFalse();
        inner.Verify(h => h.HandleAsync(It.IsAny<GetGroupQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
