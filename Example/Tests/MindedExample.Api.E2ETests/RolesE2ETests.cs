using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Application.Role.Query;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Tests.E2E.Common;
using QM.Common.Testing;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// E2E tests for role and permission management (RolesController).
    /// Runs against the real JWT pipeline: a tenant owner is registered through the public API
    /// and manages roles within their tenant, exercising the [RequirePermissions] decorators
    /// with the real authorization context (UserRoles/RolePermissions read from the database).
    /// Note: roles exist implicitly — a role appears in GET /api/roles once it has at least
    /// one permission assigned (RolePermissions rows define it).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class RolesE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        #region Read endpoints
        [TestMethod]
        public async Task GET_roles_should_return_default_roles_with_their_permissions()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            var response = await _sutClient.GetAsync("/api/roles");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var roles = await response.Content.ReadAsAsync<List<RoleDto>>();

            roles.Select(r => r.Name).Should().BeEquivalentTo(DefaultRolesDefinition.AllRoles);

            var tenantAdmin = roles.Single(r => r.Name == Roles.TenantAdmin);
            tenantAdmin.Permissions.Should().BeEquivalentTo(DefaultRolesDefinition.RolePermissions[Roles.TenantAdmin]);
        }

        [TestMethod]
        public async Task GET_permissions_should_return_all_known_permission_groups()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            var response = await _sutClient.GetAsync("/api/permissions");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var groups = await response.Content.ReadAsAsync<Dictionary<string, string[]>>();

            groups.Values.SelectMany(p => p).Should().BeEquivalentTo(DefaultRolesDefinition.AllPermissions);
        }

        [TestMethod]
        public async Task GET_roles_should_return_401Unauthorized_for_anonymous_callers()
        {
            UseAnonymous();
            var response = await _sutClient.GetAsync("/api/roles");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Create / update permissions
        [TestMethod]
        public async Task POST_roles_should_succeed_and_role_becomes_visible_once_permissions_are_assigned()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);
            var roleName = $"Role{Any.String()}";

            var create = await _sutClient.PostAsync("/api/roles", new { Name = roleName });
            create.IsSuccessStatusCode.Should().BeTrue($"role creation should succeed but was {(int)create.StatusCode}");

            // Roles exist implicitly: the new role appears once it has permissions
            var permissions = new List<string> { Permissions.CanCreateCategory, Permissions.CanUpdateCategory };
            var update = await _sutClient.PutAsync($"/api/roles/{roleName}/permissions", permissions);
            update.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var roles = await GetRolesAsync();
            var created = roles.SingleOrDefault(r => r.Name == roleName);
            created.Should().NotBeNull();
            created.Permissions.Should().BeEquivalentTo(permissions);
        }

        [TestMethod]
        public async Task PUT_role_permissions_should_replace_previously_assigned_permissions()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);
            var roleName = $"Role{Any.String()}";

            await _sutClient.PutAsync($"/api/roles/{roleName}/permissions",
                new List<string> { Permissions.CanCreateCategory, Permissions.CanDeleteCategory });

            var replacement = new List<string> { Permissions.CanDeleteCategory, Permissions.CanCreateTransaction };
            var update = await _sutClient.PutAsync($"/api/roles/{roleName}/permissions", replacement);
            update.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var roles = await GetRolesAsync();
            roles.Single(r => r.Name == roleName).Permissions.Should().BeEquivalentTo(replacement);
        }
        #endregion

        #region Delete
        [TestMethod]
        public async Task DELETE_role_should_remove_permissions_and_user_assignments()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);
            UseBearer(owner.AccessToken);

            var roleName = $"Role{Any.String()}";
            await _sutClient.PutAsync($"/api/roles/{roleName}/permissions",
                new List<string> { Permissions.CanCreateCategory });
            await _sutClient.PutAsync($"/api/users/{member.User.Id}/roles", new List<string> { roleName });

            var delete = await _sutClient.DeleteAsync($"/api/roles/{roleName}");
            delete.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var roles = await GetRolesAsync();
            roles.Should().NotContain(r => r.Name == roleName);

            var usersWithRoles = await GetUsersWithRolesAsync();
            usersWithRoles.Single(u => u.Id == member.User.Id).Roles.Should().NotContain(roleName);
        }
        #endregion

        #region Assign roles to users
        [TestMethod]
        public async Task PUT_users_roles_should_assign_roles_visible_in_users_with_roles()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);
            UseBearer(owner.AccessToken);

            var assign = await _sutClient.PutAsync($"/api/users/{member.User.Id}/roles",
                new List<string> { Roles.TenantAdmin });
            assign.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var usersWithRoles = await GetUsersWithRolesAsync();
            usersWithRoles.Single(u => u.Id == member.User.Id).Roles.Should().BeEquivalentTo(new[] { Roles.TenantAdmin });
        }

        [TestMethod]
        public async Task PUT_users_roles_should_replace_existing_assignments()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);
            UseBearer(owner.AccessToken);

            // Invited members start with the default User role assigned by the accept-invite flow
            var initial = await GetUsersWithRolesAsync();
            initial.Single(u => u.Id == member.User.Id).Roles.Should().BeEquivalentTo(new[] { Roles.User });

            await _sutClient.PutAsync($"/api/users/{member.User.Id}/roles", new List<string> { Roles.TenantAdmin });

            var updated = await GetUsersWithRolesAsync();
            updated.Single(u => u.Id == member.User.Id).Roles.Should().BeEquivalentTo(new[] { Roles.TenantAdmin });
        }
        #endregion

        #region Reset to default
        [TestMethod]
        public async Task POST_reset_to_default_should_restore_default_role_permissions()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            // Drift from the defaults: shrink TenantAdmin (keeping the permissions the owner
            // needs to keep managing roles — removing them would lock the owner out) and add
            // a custom role
            await _sutClient.PutAsync($"/api/roles/{Roles.TenantAdmin}/permissions",
                new List<string>
                {
                    Permissions.CanCreateCategory,
                    Permissions.CanManageRoles,
                    Permissions.CanUpdateRolePermissions
                });
            var customRole = $"Role{Any.String()}";
            await _sutClient.PutAsync($"/api/roles/{customRole}/permissions",
                new List<string> { Permissions.CanCreateTransaction });

            var reset = await _sutClient.PostAsync("/api/roles/reset-to-default", new { });
            reset.IsSuccessStatusCode.Should().BeTrue($"reset should succeed but was {(int)reset.StatusCode}");

            var roles = await GetRolesAsync();
            roles.Select(r => r.Name).Should().BeEquivalentTo(DefaultRolesDefinition.AllRoles);
            roles.Single(r => r.Name == Roles.TenantAdmin).Permissions
                .Should().BeEquivalentTo(DefaultRolesDefinition.RolePermissions[Roles.TenantAdmin]);
        }
        #endregion

        #region Authorization
        [TestMethod]
        public async Task POST_roles_should_return_403Forbidden_for_member_without_role_management_permission()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);

            UseBearer(member.AccessToken);
            var response = await _sutClient.PostAsync("/api/roles", new { Name = $"Role{Any.String()}" });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task POST_reset_to_default_should_return_403Forbidden_for_member_without_permission()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);

            UseBearer(member.AccessToken);
            var response = await _sutClient.PostAsync("/api/roles/reset-to-default", new { });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
        }
        #endregion

        #region Helpers
        private async Task<List<RoleDto>> GetRolesAsync()
        {
            var response = await _sutClient.GetAsync("/api/roles");
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            return await response.Content.ReadAsAsync<List<RoleDto>>();
        }

        private async Task<List<User>> GetUsersWithRolesAsync()
        {
            var response = await _sutClient.GetAsync("/api/users-with-roles");
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            return await response.Content.ReadAsAsync<List<User>>();
        }
        #endregion
    }
}
