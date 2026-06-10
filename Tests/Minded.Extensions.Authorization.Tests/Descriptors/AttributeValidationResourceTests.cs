using System;
using FluentAssertions;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Authorization.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Authorization.Tests.Descriptors;

[TestClass]
public class AttributeValidationResourceTests
{
    [RequireClaim("", "x")]
    private class BlankClaimTypeRequest
    {
    }

    [RequireClaim("Region")]
    private class ClaimWithoutValuesOrMatchPropertyRequest
    {
    }

    [RequireClaim("Tenant", MatchProperty = "Missing")]
    private class ClaimWithMissingPropertyRequest
    {
        public string TenantId { get; set; } = string.Empty;
    }

    [RequireResourceAccess("Missing", "UserId", typeof(ValidResourceQuery))]
    private class ResourceMissingPropertyRequest
    {
        public Guid Id { get; set; }
    }

    [RequireResourceAccess(nameof(ResourceInvalidQueryTypeRequest.Id), "UserId", typeof(string))]
    private class ResourceInvalidQueryTypeRequest
    {
        public Guid Id { get; set; }
    }

    [AllowUnauthenticated]
    [RequireResourceAccess(nameof(ResourceWithAllowUnauthenticatedRequest.Id), "UserId", typeof(ValidResourceQuery))]
    private class ResourceWithAllowUnauthenticatedRequest
    {
        public Guid Id { get; set; }
    }

    [RequireRoles("Admin", OrAnyRole = new[] { " " })]
    private class InvalidOrRoleRequest
    {
    }

    [RequirePermissions("perm", OrAnyPermission = new[] { "" })]
    private class InvalidOrPermissionRequest
    {
    }

    [RequireClaim("Region", "EU", OrAnyClaim = new[] { "  " })]
    private class InvalidOrClaimRequest
    {
    }

    private class ValidResourceQuery : IQuery<bool>
    {
        public ValidResourceQuery(object resourceId, string claimValue)
        {
        }

        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [TestMethod]
    public void Validate_InvalidClaimConfiguration_Throws()
    {
        Action act1 = () => AttributeValidator.Validate(typeof(BlankClaimTypeRequest));
        Action act2 = () => AttributeValidator.Validate(typeof(ClaimWithoutValuesOrMatchPropertyRequest));
        Action act3 = () => AttributeValidator.Validate(typeof(ClaimWithMissingPropertyRequest));

        act1.Should().Throw<InvalidOperationException>();
        act2.Should().Throw<InvalidOperationException>();
        act3.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Validate_InvalidResourceConfiguration_Throws()
    {
        Action act1 = () => AttributeValidator.Validate(typeof(ResourceMissingPropertyRequest));
        Action act2 = () => AttributeValidator.Validate(typeof(ResourceInvalidQueryTypeRequest));
        Action act3 = () => AttributeValidator.Validate(typeof(ResourceWithAllowUnauthenticatedRequest));

        act1.Should().Throw<InvalidOperationException>();
        act2.Should().Throw<InvalidOperationException>();
        act3.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Validate_InvalidOrArrays_Throw()
    {
        Action act1 = () => AttributeValidator.Validate(typeof(InvalidOrRoleRequest));
        Action act2 = () => AttributeValidator.Validate(typeof(InvalidOrPermissionRequest));
        Action act3 = () => AttributeValidator.Validate(typeof(InvalidOrClaimRequest));

        act1.Should().Throw<InvalidOperationException>();
        act2.Should().Throw<InvalidOperationException>();
        act3.Should().Throw<InvalidOperationException>();
    }
}
