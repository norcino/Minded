using System;
using FluentAssertions;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Authorization.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Authorization.Tests.Descriptors;

[TestClass]
public class AuthorizationDescriptorCacheResourceTests
{
    [TestInitialize]
    public void Setup()
    {
        AuthorizationDescriptorCache.Clear();
    }

    [RequireClaim("Region", "EU", Match = AuthorizationMatch.Any, OrAnyRole = new[] { "Admin" })]
    [RequireResourceAccess(nameof(ResourceRequest.ProjectId), "UserId", typeof(CanAccessProjectQuery), OrAnyPermission = new[] { "projects.full" })]
    private class ResourceRequest
    {
        public Guid ProjectId { get; set; }
    }

    [RequireResourceAccess("MissingProperty", "UserId", typeof(CanAccessProjectQuery))]
    private class InvalidResourcePropertyRequest
    {
        public Guid ProjectId { get; set; }
    }

    [RequireResourceAccess(nameof(InvalidResourceQueryRequest.ProjectId), "UserId", typeof(InvalidAuthorizationQuery))]
    private class InvalidResourceQueryRequest
    {
        public Guid ProjectId { get; set; }
    }

    private class CanAccessProjectQuery : IQuery<bool>
    {
        public CanAccessProjectQuery(object resourceId, string claimValue)
        {
            ResourceId = resourceId;
            ClaimValue = claimValue;
        }

        public object ResourceId { get; }
        public string ClaimValue { get; }
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    private class InvalidAuthorizationQuery
    {
        public InvalidAuthorizationQuery(object resourceId, string claimValue)
        {
        }
    }

    [TestMethod]
    public void Compile_ClaimAndResourceAttributes_AreIncludedInDescriptor()
    {
        var descriptor = AuthorizationDescriptorCache.GetOrCreate(typeof(ResourceRequest));

        descriptor.IsProtected.Should().BeTrue();
        descriptor.ClaimClauses.Should().ContainSingle();
        descriptor.ResourceClauses.Should().ContainSingle();

        descriptor.ClaimClauses[0].ClaimType.Should().Be("Region");
        descriptor.ClaimClauses[0].Values.Should().ContainSingle().Which.Should().Be("EU");
        descriptor.ClaimClauses[0].OrAnyRole.Should().ContainSingle().Which.Should().Be("Admin");

        descriptor.ResourceClauses[0].ResourceIdProperty.Should().Be(nameof(ResourceRequest.ProjectId));
        descriptor.ResourceClauses[0].ResourceIdClaim.Should().Be("UserId");
        descriptor.ResourceClauses[0].QueryType.Should().Be(typeof(CanAccessProjectQuery));
        descriptor.ResourceClauses[0].OrAnyPermission.Should().ContainSingle().Which.Should().Be("projects.full");
    }

    [TestMethod]
    public void Compile_InvalidResourceProperty_Throws()
    {
        Action act = () => AuthorizationDescriptorCache.GetOrCreate(typeof(InvalidResourcePropertyRequest));

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Compile_InvalidResourceQueryType_Throws()
    {
        Action act = () => AuthorizationDescriptorCache.GetOrCreate(typeof(InvalidResourceQueryRequest));

        act.Should().Throw<InvalidOperationException>();
    }
}
