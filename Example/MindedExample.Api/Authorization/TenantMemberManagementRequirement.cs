using Microsoft.AspNetCore.Authorization;

namespace MindedExample.Api.Authorization
{
    /// <summary>
    /// Authorization requirement that grants access only to users who can manage tenant members.
    /// Satisfied when the authenticated user is a tenant Owner or Admin, or holds the TenantAdmin application role.
    /// </summary>
    public class TenantMemberManagementRequirement : IAuthorizationRequirement
    {
    }
}
