using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Api.Models;
using MindedExample.Application.User.Query;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Tests.E2E.Common;
using QM.Common.Testing;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// E2E tests for tenant administration (TenantAdminController): member listing, tenant role
    /// changes, member removal, and join-request approval/rejection. Runs against the real JWT
    /// pipeline with owners and members registered through the public API, exercising the
    /// TenantMemberManagement authorization policy for real.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TenantAdminE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        #region Member listing
        [TestMethod]
        public async Task GET_users_should_return_only_current_tenant_members()
        {
            var ownerA = await RegisterTenantOwnerAsync();
            var memberA = await RegisterInvitedMemberAsync(ownerA.AccessToken);

            // A second tenant whose members must not leak into tenant A's view
            var ownerB = await RegisterTenantOwnerAsync();
            await RegisterInvitedMemberAsync(ownerB.AccessToken);

            UseBearer(ownerA.AccessToken);
            var members = await GetTenantUsersAsync();

            members.Select(m => m.Email).Should().BeEquivalentTo(new[] { ownerA.User.Email, memberA.User.Email });
            members.Should().OnlyContain(m => m.TenantId == ownerA.User.TenantId);
        }

        [TestMethod]
        public async Task GET_users_should_return_403Forbidden_for_plain_member()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);

            UseBearer(member.AccessToken);
            var response = await _sutClient.GetAsync("/api/tenant-admin/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task GET_users_should_return_401Unauthorized_for_anonymous_callers()
        {
            UseAnonymous();
            var response = await _sutClient.GetAsync("/api/tenant-admin/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Member tenant role
        [TestMethod]
        public async Task PUT_users_role_should_change_member_role_and_grant_admin_access()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);

            UseBearer(owner.AccessToken);
            var update = await _sutClient.PutAsync($"/api/tenant-admin/users/{member.User.Id}/role",
                new UpdateTenantUserRoleRequest { Role = TenantMemberRoles.Admin });
            update.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var members = await GetTenantUsersAsync();
            members.Single(m => m.Id == member.User.Id).TenantRole.Should().Be(TenantMemberRoles.Admin);

            // A tenant Admin passes the TenantMemberManagement policy
            UseBearer(member.AccessToken);
            var asAdmin = await _sutClient.GetAsync("/api/tenant-admin/users");
            asAdmin.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task PUT_users_role_should_fail_for_user_of_another_tenant()
        {
            var ownerA = await RegisterTenantOwnerAsync();
            var memberA = await RegisterInvitedMemberAsync(ownerA.AccessToken);
            var ownerB = await RegisterTenantOwnerAsync();

            // Cross-tenant target: the command does not find the user within B's tenant
            UseBearer(ownerB.AccessToken);
            var response = await _sutClient.PutAsync($"/api/tenant-admin/users/{memberA.User.Id}/role",
                new UpdateTenantUserRoleRequest { Role = TenantMemberRoles.Admin });

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);

            // And the member's role is untouched
            UseBearer(ownerA.AccessToken);
            var members = await GetTenantUsersAsync();
            members.Single(m => m.Id == memberA.User.Id).TenantRole.Should().Be(TenantMemberRoles.Member);
        }
        #endregion

        #region Member removal
        [TestMethod]
        public async Task DELETE_users_should_remove_member_from_tenant()
        {
            var owner = await RegisterTenantOwnerAsync();
            var memberEmail = Any.Email();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken, memberEmail);

            UseBearer(owner.AccessToken);
            var delete = await _sutClient.DeleteAsync($"/api/tenant-admin/users/{member.User.Id}");
            delete.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var members = await GetTenantUsersAsync();
            members.Should().NotContain(m => m.Id == member.User.Id);

            // The account is gone entirely: the removed member can no longer log in
            UseAnonymous();
            var login = await _sutClient.PostAsync("/api/auth/login",
                new LoginRequest { Email = memberEmail, Password = DefaultTestPassword });
            login.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task DELETE_users_should_not_allow_removing_the_legal_owner()
        {
            var owner = await RegisterTenantOwnerAsync();

            UseBearer(owner.AccessToken);
            var response = await _sutClient.DeleteAsync($"/api/tenant-admin/users/{owner.User.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Join requests
        [TestMethod]
        public async Task POST_join_request_approve_should_create_account_able_to_login()
        {
            var owner = await RegisterTenantOwnerAsync();
            var joinerEmail = Any.Email();
            await RegisterJoinRequestAsync(owner.Tenant.Name, joinerEmail);

            UseBearer(owner.AccessToken);
            var pending = await GetJoinRequestsAsync();
            var request = pending.Single(r => r.Email == joinerEmail.ToLowerInvariant());

            var approve = await _sutClient.PostAsync($"/api/tenant-admin/join-requests/{request.Id}/approve", new { });
            approve.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            (await GetJoinRequestsAsync()).Should().BeEmpty();

            UseAnonymous();
            var login = await _sutClient.PostAsync("/api/auth/login",
                new LoginRequest { Email = joinerEmail, Password = DefaultTestPassword });
            login.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var auth = await login.Content.ReadAsAsync<AuthResponse>();
            auth.User.TenantId.Should().Be(owner.User.TenantId);
            auth.User.TenantRole.Should().Be(TenantMemberRoles.Member);
        }

        [TestMethod]
        public async Task POST_join_request_reject_should_keep_the_applicant_blocked()
        {
            var owner = await RegisterTenantOwnerAsync();
            var joinerEmail = Any.Email();
            await RegisterJoinRequestAsync(owner.Tenant.Name, joinerEmail);

            UseBearer(owner.AccessToken);
            var pending = await GetJoinRequestsAsync();
            var request = pending.Single(r => r.Email == joinerEmail.ToLowerInvariant());

            var reject = await _sutClient.PostAsync($"/api/tenant-admin/join-requests/{request.Id}/reject", new { });
            reject.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            (await GetJoinRequestsAsync()).Should().BeEmpty();

            UseAnonymous();
            var login = await _sutClient.PostAsync("/api/auth/login",
                new LoginRequest { Email = joinerEmail, Password = DefaultTestPassword });
            login.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Helpers
        private async Task RegisterJoinRequestAsync(string tenantName, string email)
        {
            UseAnonymous();
            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = email,
                Password = DefaultTestPassword,
                TenantName = tenantName,
                Mode = "join-tenant"
            });
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<TenantAdminUserDto>> GetTenantUsersAsync()
        {
            var response = await _sutClient.GetAsync("/api/tenant-admin/users");
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            return await response.Content.ReadAsAsync<List<TenantAdminUserDto>>();
        }

        private async Task<List<TenantJoinRequestSummaryDto>> GetJoinRequestsAsync()
        {
            var response = await _sutClient.GetAsync("/api/tenant-admin/join-requests");
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            return await response.Content.ReadAsAsync<List<TenantJoinRequestSummaryDto>>();
        }
        #endregion
    }
}
