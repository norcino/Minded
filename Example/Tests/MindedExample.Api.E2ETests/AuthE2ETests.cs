using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Api.Models;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Tests.E2E.Common;
using QM.Common.Testing;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// E2E tests for the authentication endpoints (register, login, password reset, invites, me).
    /// Unlike the other E2E suites these run against the REAL JWT bearer pipeline
    /// (UseTestAuthentication = false): requests are anonymous unless a token obtained
    /// from the API itself is attached.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AuthE2ETests : BaseE2ETest
    {
        private const string DefaultPassword = DefaultTestPassword;

        protected override bool UseTestAuthentication => false;

        #region Register - create-tenant
        [TestMethod]
        public async Task POST_register_create_tenant_should_return_token_and_owner_profile()
        {
            var email = Any.Email();
            var tenantName = $"Tenant {Any.String()}";

            var auth = await RegisterTenantOwnerAsync(email, DefaultPassword, tenantName);

            auth.AccessToken.Should().NotBeNullOrWhiteSpace();
            auth.User.Email.Should().Be(email.ToLowerInvariant());
            auth.User.TenantRole.Should().Be(TenantMemberRoles.Owner);
            auth.User.IsGlobalAdmin.Should().BeFalse();
            auth.Tenant.Name.Should().Be(tenantName);

            // The tenant exists and is legally owned by the new user
            var tenant = await Context.Tenants.AsNoTracking().SingleAsync(t => t.Id == auth.Tenant.Id);
            tenant.LegalOwnerUserId.Should().Be(auth.User.Id);
        }

        [TestMethod]
        public async Task POST_register_create_tenant_token_should_authenticate_me_endpoint()
        {
            var email = Any.Email();
            var auth = await RegisterTenantOwnerAsync(email, DefaultPassword);

            UseBearer(auth.AccessToken);
            var response = await _sutClient.GetAsync("/api/auth/me");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var me = await response.Content.ReadAsAsync<AuthResponse>();
            me.User.Email.Should().Be(email.ToLowerInvariant());
            me.User.TenantId.Should().Be(auth.User.TenantId);
        }

        [TestMethod]
        public async Task POST_register_should_return_409Conflict_when_email_already_registered()
        {
            var email = Any.Email();
            await RegisterTenantOwnerAsync(email, DefaultPassword);

            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = email,
                Password = DefaultPassword,
                TenantName = $"Tenant {Any.String()}"
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Conflict);
        }

        [TestMethod]
        public async Task POST_register_should_return_400BadRequest_when_password_is_missing()
        {
            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = Any.Email(),
                Password = null
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Register - join-tenant
        [TestMethod]
        public async Task POST_register_join_tenant_should_create_pending_join_request()
        {
            var tenantName = $"Tenant {Any.String()}";
            await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword, tenantName);

            var joinerEmail = Any.Email();
            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = joinerEmail,
                Password = DefaultPassword,
                TenantName = tenantName,
                Mode = "join-tenant"
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var pending = await Context.TenantJoinRequests.AsNoTracking()
                .SingleAsync(r => r.Email == joinerEmail.ToLowerInvariant());
            pending.ProcessedAtUtc.Should().BeNull();
            pending.Approved.Should().BeFalse();
        }

        [TestMethod]
        public async Task POST_login_should_return_401Unauthorized_before_join_request_is_approved()
        {
            var tenantName = $"Tenant {Any.String()}";
            await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword, tenantName);

            var joinerEmail = Any.Email();
            await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = joinerEmail,
                Password = DefaultPassword,
                TenantName = tenantName,
                Mode = "join-tenant"
            });

            var login = await _sutClient.PostAsync("/api/auth/login", new LoginRequest
            {
                Email = joinerEmail,
                Password = DefaultPassword
            });

            login.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task POST_register_join_tenant_should_return_404NotFound_when_tenant_does_not_exist()
        {
            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = Any.Email(),
                Password = DefaultPassword,
                TenantName = $"NonExistent {Any.String()}",
                Mode = "join-tenant"
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task POST_register_should_return_409Conflict_when_pending_join_request_exists()
        {
            var tenantName = $"Tenant {Any.String()}";
            await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword, tenantName);

            var joinerEmail = Any.Email();
            await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = joinerEmail,
                Password = DefaultPassword,
                TenantName = tenantName,
                Mode = "join-tenant"
            });

            // Second registration with the same email, regardless of mode
            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = joinerEmail,
                Password = DefaultPassword,
                TenantName = $"Tenant {Any.String()}"
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Conflict);
        }
        #endregion

        #region Login
        [TestMethod]
        public async Task POST_login_should_return_token_usable_on_protected_endpoint()
        {
            var email = Any.Email();
            await RegisterTenantOwnerAsync(email, DefaultPassword);

            UseAnonymous();
            var login = await _sutClient.PostAsync("/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = DefaultPassword
            });

            login.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var auth = await login.Content.ReadAsAsync<AuthResponse>();
            auth.AccessToken.Should().NotBeNullOrWhiteSpace();

            UseBearer(auth.AccessToken);
            var me = await _sutClient.GetAsync("/api/auth/me");
            me.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task POST_login_should_return_401Unauthorized_when_password_is_wrong()
        {
            var email = Any.Email();
            await RegisterTenantOwnerAsync(email, DefaultPassword);

            var login = await _sutClient.PostAsync("/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = "WrongPassword1!"
            });

            login.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task POST_login_should_return_401Unauthorized_when_email_is_unknown()
        {
            // Same status as a wrong password: the response must not reveal whether the account exists
            var login = await _sutClient.PostAsync("/api/auth/login", new LoginRequest
            {
                Email = Any.Email(),
                Password = DefaultPassword
            });

            login.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task POST_login_should_return_400BadRequest_when_password_is_missing()
        {
            var login = await _sutClient.PostAsync("/api/auth/login", new LoginRequest
            {
                Email = Any.Email(),
                Password = null
            });

            login.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Me
        [TestMethod]
        public async Task GET_me_should_return_401Unauthorized_without_token()
        {
            UseAnonymous();
            var response = await _sutClient.GetAsync("/api/auth/me");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task GET_me_should_return_401Unauthorized_with_invalid_token()
        {
            UseBearer("not-a-valid-jwt");
            var response = await _sutClient.GetAsync("/api/auth/me");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Forgot / reset password
        [TestMethod]
        public async Task POST_forgot_password_should_issue_token_and_reset_should_rotate_password()
        {
            var email = Any.Email();
            const string newPassword = "NewPassw0rd!";
            var auth = await RegisterTenantOwnerAsync(email, DefaultPassword);

            UseAnonymous();
            var forgot = await _sutClient.PostAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = email });
            forgot.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            // No email infrastructure in the example app: read the token from the database
            var tokenRow = await Context.PasswordResetTokens.AsNoTracking()
                .Where(t => t.UserId == auth.User.Id)
                .OrderByDescending(t => t.Id)
                .FirstAsync();

            var reset = await _sutClient.PostAsync("/api/auth/reset-password", new ResetPasswordRequest
            {
                Token = tokenRow.Token,
                NewPassword = newPassword
            });
            reset.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var oldLogin = await _sutClient.PostAsync("/api/auth/login", new LoginRequest { Email = email, Password = DefaultPassword });
            oldLogin.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);

            var newLogin = await _sutClient.PostAsync("/api/auth/login", new LoginRequest { Email = email, Password = newPassword });
            newLogin.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task POST_forgot_password_should_return_200Ok_without_issuing_token_for_unknown_email()
        {
            // 200 regardless of account existence: the response must not reveal whether the account exists
            var forgot = await _sutClient.PostAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = Any.Email() });

            forgot.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            (await Context.PasswordResetTokens.AsNoTracking().AnyAsync()).Should().BeFalse();
        }

        [TestMethod]
        public async Task POST_reset_password_should_return_400BadRequest_for_invalid_token()
        {
            var reset = await _sutClient.PostAsync("/api/auth/reset-password", new ResetPasswordRequest
            {
                Token = "not-a-real-token",
                NewPassword = "NewPassw0rd!"
            });

            reset.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task POST_reset_password_should_return_400BadRequest_when_token_already_used()
        {
            var email = Any.Email();
            var auth = await RegisterTenantOwnerAsync(email, DefaultPassword);

            UseAnonymous();
            await _sutClient.PostAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = email });
            var tokenRow = await Context.PasswordResetTokens.AsNoTracking()
                .Where(t => t.UserId == auth.User.Id)
                .OrderByDescending(t => t.Id)
                .FirstAsync();

            var firstReset = await _sutClient.PostAsync("/api/auth/reset-password", new ResetPasswordRequest
            {
                Token = tokenRow.Token,
                NewPassword = "NewPassw0rd!"
            });
            firstReset.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var secondReset = await _sutClient.PostAsync("/api/auth/reset-password", new ResetPasswordRequest
            {
                Token = tokenRow.Token,
                NewPassword = "AnotherPassw0rd!"
            });
            secondReset.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Invites
        [TestMethod]
        public async Task Invite_round_trip_should_let_member_login_scoped_to_inviting_tenant()
        {
            var owner = await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword);
            var memberEmail = Any.Email();

            var invite = await CreateInviteAsync(owner.AccessToken, memberEmail);
            invite.Token.Should().NotBeNullOrWhiteSpace();
            invite.Code.Should().NotBeNullOrWhiteSpace();

            // Public invite resolution shows the tenant the member is joining
            UseAnonymous();
            var details = await _sutClient.GetAsync($"/api/auth/invite/{invite.Token}");
            details.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var resolution = await details.Content.ReadAsAsync<InviteResolutionDto>();
            resolution.TenantName.Should().Be(owner.Tenant.Name);

            var accept = await _sutClient.PostAsync("/api/auth/accept-invite", new AcceptInviteRequest
            {
                CodeOrToken = invite.Token,
                Email = memberEmail,
                Name = Any.String(),
                Surname = Any.String(),
                Password = DefaultPassword
            });
            accept.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var memberAuth = await accept.Content.ReadAsAsync<AuthResponse>();
            memberAuth.User.TenantId.Should().Be(owner.User.TenantId);
            memberAuth.User.TenantRole.Should().Be(TenantMemberRoles.Member);

            var login = await _sutClient.PostAsync("/api/auth/login", new LoginRequest
            {
                Email = memberEmail,
                Password = DefaultPassword
            });
            login.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var loginAuth = await login.Content.ReadAsAsync<AuthResponse>();
            loginAuth.User.TenantId.Should().Be(owner.User.TenantId);
        }

        [TestMethod]
        public async Task POST_accept_invite_should_return_400BadRequest_when_invite_already_used()
        {
            var owner = await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword);
            var memberEmail = Any.Email();
            var invite = await CreateInviteAsync(owner.AccessToken, memberEmail);

            UseAnonymous();
            var firstAccept = await _sutClient.PostAsync("/api/auth/accept-invite", new AcceptInviteRequest
            {
                CodeOrToken = invite.Token,
                Email = memberEmail,
                Name = Any.String(),
                Surname = Any.String(),
                Password = DefaultPassword
            });
            firstAccept.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var secondAccept = await _sutClient.PostAsync("/api/auth/accept-invite", new AcceptInviteRequest
            {
                CodeOrToken = invite.Token,
                Email = Any.Email(),
                Name = Any.String(),
                Surname = Any.String(),
                Password = DefaultPassword
            });
            secondAccept.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task POST_accept_invite_should_return_400BadRequest_when_email_does_not_match_invite()
        {
            var owner = await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword);
            var invite = await CreateInviteAsync(owner.AccessToken, Any.Email());

            UseAnonymous();
            var accept = await _sutClient.PostAsync("/api/auth/accept-invite", new AcceptInviteRequest
            {
                CodeOrToken = invite.Token,
                Email = Any.Email(),
                Name = Any.String(),
                Surname = Any.String(),
                Password = DefaultPassword
            });

            accept.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task GET_invite_details_should_return_404NotFound_for_unknown_token()
        {
            var response = await _sutClient.GetAsync($"/api/auth/invite/{Any.String()}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task POST_register_with_invite_token_should_create_member_in_inviting_tenant()
        {
            var owner = await RegisterTenantOwnerAsync(Any.Email(), DefaultPassword);
            var memberEmail = Any.Email();
            var invite = await CreateInviteAsync(owner.AccessToken, memberEmail);

            UseAnonymous();
            var response = await _sutClient.PostAsync("/api/auth/register", new RegisterRequest
            {
                Name = Any.String(),
                Surname = Any.String(),
                Email = memberEmail,
                Password = DefaultPassword,
                InviteToken = invite.Token
            });

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var memberAuth = await response.Content.ReadAsAsync<AuthResponse>();
            memberAuth.User.TenantId.Should().Be(owner.User.TenantId);
            memberAuth.User.TenantRole.Should().Be(TenantMemberRoles.Member);
        }
        #endregion

    }
}
