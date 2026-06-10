using System.Collections.Generic;

namespace MindedExample.Api.Models
{
    public class RegisterRequest
    {
        public string Name { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string TenantName { get; set; }

        public string Mode { get; set; }

        public string InviteToken { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; }

        public string NewPassword { get; set; }
    }

    public class AcceptInviteRequest
    {
        public string CodeOrToken { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }
    }

    public class CreateInviteRequest
    {
        public string Email { get; set; }
    }

    public class UpdateTenantUserRoleRequest
    {
        public string Role { get; set; }
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; }

        public AuthUserDto User { get; set; }

        public TenantDto Tenant { get; set; }
    }

    public class AuthUserDto
    {
        public int Id { get; set; }

        public int? TenantId { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public string TenantRole { get; set; }

        public bool IsGlobalAdmin { get; set; }

        public IReadOnlyCollection<string> Roles { get; set; }
    }

    public class TenantDto
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class TenantSummaryDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? LegalOwnerUserId { get; set; }

        public string LegalOwnerEmail { get; set; }

        public int ActiveUsersCount { get; set; }

        public int CategoriesCount { get; set; }

        public int TransactionsCount { get; set; }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; }

        public string LegalOwnerName { get; set; }

        public string LegalOwnerSurname { get; set; }

        public string LegalOwnerEmail { get; set; }

        public string LegalOwnerPassword { get; set; }
    }

    public class DeleteTenantRequest
    {
        public string ConfirmationName { get; set; }
    }

    public class TenantJoinRequestDto
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public string CreatedAtUtc { get; set; }
    }

    public class InviteResolutionDto
    {
        public string TenantName { get; set; }

        public string Email { get; set; }
    }

    public class TenantInviteDto
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string Code { get; set; }

        public string Token { get; set; }

        public string InviteLink { get; set; }

        public string ExpiresAtUtc { get; set; }
    }
}
