using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Api.Models;
using MindedExample.Tests.Common;
using MindedExample.Tests.E2E.Common;
using QM.Common.Testing;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// E2E tests for global tenant management (TenantsController, GlobalAdminOnly policy).
    /// Global admins are provisioned directly in the database (no public API exists for that);
    /// everything else — tenant creation, deletion with confirmation, listing — runs through
    /// the real JWT pipeline.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TenantsE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        #region Listing
        [TestMethod]
        public async Task GET_tenants_should_return_summaries_including_owner_and_user_count()
        {
            var owner = await RegisterTenantOwnerAsync();
            await RegisterInvitedMemberAsync(owner.AccessToken);
            var admin = await CreateGlobalAdminAsync();

            UseBearer(admin.AccessToken);
            var response = await _sutClient.GetAsync("/api/tenants");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var summaries = await response.Content.ReadAsAsync<List<TenantSummaryDto>>();

            var summary = summaries.Single(s => s.Id == owner.Tenant.Id);
            summary.Name.Should().Be(owner.Tenant.Name);
            summary.LegalOwnerEmail.Should().Be(owner.User.Email);
            summary.ActiveUsersCount.Should().Be(2);
        }

        [TestMethod]
        public async Task GET_tenants_should_work_identically_on_the_admin_route_prefix()
        {
            var owner = await RegisterTenantOwnerAsync();
            var admin = await CreateGlobalAdminAsync();

            UseBearer(admin.AccessToken);
            var canonical = await _sutClient.GetAsync("/api/tenants");
            var aliased = await _sutClient.GetAsync("/api/admin/tenants");

            canonical.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            aliased.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var canonicalIds = (await canonical.Content.ReadAsAsync<List<TenantSummaryDto>>()).Select(t => t.Id);
            var aliasedIds = (await aliased.Content.ReadAsAsync<List<TenantSummaryDto>>()).Select(t => t.Id);
            aliasedIds.Should().BeEquivalentTo(canonicalIds);
            canonicalIds.Should().Contain(owner.Tenant.Id);
        }

        [TestMethod]
        public async Task GET_tenants_should_return_403Forbidden_for_tenant_owner()
        {
            var owner = await RegisterTenantOwnerAsync();

            UseBearer(owner.AccessToken);
            var response = await _sutClient.GetAsync("/api/tenants");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task GET_tenants_should_return_401Unauthorized_for_anonymous_callers()
        {
            UseAnonymous();
            var response = await _sutClient.GetAsync("/api/tenants");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Creation
        [TestMethod]
        public async Task POST_tenants_should_create_tenant_whose_legal_owner_can_login()
        {
            var admin = await CreateGlobalAdminAsync();
            var tenantName = $"Tenant {Any.String()}";
            var ownerEmail = Any.Email();

            UseBearer(admin.AccessToken);
            var create = await _sutClient.PostAsync("/api/tenants", new CreateTenantRequest
            {
                Name = tenantName,
                LegalOwnerName = Any.String(),
                LegalOwnerSurname = Any.String(),
                LegalOwnerEmail = ownerEmail,
                LegalOwnerPassword = DefaultTestPassword
            });
            create.Should().HaveHttpStatusCode(HttpStatusCode.Created);

            var summaries = await GetTenantSummariesAsync();
            summaries.Should().Contain(s => s.Name == tenantName && s.LegalOwnerEmail == ownerEmail.ToLowerInvariant());

            UseAnonymous();
            var ownerLogin = await _sutClient.PostAsync("/api/auth/login",
                new LoginRequest { Email = ownerEmail, Password = DefaultTestPassword });
            ownerLogin.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var ownerAuth = await ownerLogin.Content.ReadAsAsync<AuthResponse>();
            ownerAuth.Tenant.Name.Should().Be(tenantName);
        }

        [TestMethod]
        public async Task POST_tenants_should_return_403Forbidden_for_tenant_owner()
        {
            var owner = await RegisterTenantOwnerAsync();

            UseBearer(owner.AccessToken);
            var response = await _sutClient.PostAsync("/api/tenants", new CreateTenantRequest
            {
                Name = $"Tenant {Any.String()}",
                LegalOwnerName = Any.String(),
                LegalOwnerSurname = Any.String(),
                LegalOwnerEmail = Any.Email(),
                LegalOwnerPassword = DefaultTestPassword
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
        }
        #endregion

        #region Deletion
        [TestMethod]
        public async Task DELETE_tenants_should_return_400BadRequest_when_confirmation_name_is_wrong()
        {
            var owner = await RegisterTenantOwnerAsync();
            var admin = await CreateGlobalAdminAsync();

            UseBearer(admin.AccessToken);
            var response = await _sutClient.SendAsync(BuildDeleteRequest(owner.Tenant.Id, "Not the tenant name"));

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);

            // Tenant is untouched
            var summaries = await GetTenantSummariesAsync();
            summaries.Should().Contain(s => s.Id == owner.Tenant.Id);
        }

        [TestMethod]
        public async Task DELETE_tenants_should_remove_tenant_and_all_scoped_data()
        {
            var ownerEmail = Any.Email();
            var owner = await RegisterTenantOwnerAsync(ownerEmail);
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);

            // Tenant-scoped business data: a category created by the owner
            UseBearer(owner.AccessToken);
            var categoryResponse = await _sutClient.PostAsync("/api/category",
                new { Name = Any.String(), Description = Any.String(), Active = true, UserId = owner.User.Id });
            categoryResponse.Should().HaveHttpStatusCode(HttpStatusCode.Created);

            var admin = await CreateGlobalAdminAsync();
            UseBearer(admin.AccessToken);
            var delete = await _sutClient.SendAsync(BuildDeleteRequest(owner.Tenant.Id, owner.Tenant.Name));
            delete.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            // Gone from the admin listing, and all scoped rows are removed
            var summaries = await GetTenantSummariesAsync();
            summaries.Should().NotContain(s => s.Id == owner.Tenant.Id);

            (await Context.Users.AsNoTracking().AnyAsync(u => u.TenantId == owner.Tenant.Id)).Should().BeFalse();
            (await Context.Categories.AsNoTracking().AnyAsync(c => c.UserId == owner.User.Id)).Should().BeFalse();

            // The tenant's users can no longer log in
            UseAnonymous();
            var ownerLogin = await _sutClient.PostAsync("/api/auth/login",
                new LoginRequest { Email = ownerEmail, Password = DefaultTestPassword });
            ownerLogin.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Helpers
        private async Task<List<TenantSummaryDto>> GetTenantSummariesAsync()
        {
            var response = await _sutClient.GetAsync("/api/tenants");
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            return await response.Content.ReadAsAsync<List<TenantSummaryDto>>();
        }

        private static System.Net.Http.HttpRequestMessage BuildDeleteRequest(int tenantId, string confirmationName)
        {
            return new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, $"/api/tenants/{tenantId}")
            {
                Content = new System.Net.Http.StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new DeleteTenantRequest { ConfirmationName = confirmationName }),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
        #endregion
    }
}
