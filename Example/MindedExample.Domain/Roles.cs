namespace MindedExample.Domain
{
    /// <summary>
    /// Centralized role name constants used in authorization attributes and database seeding.
    /// Each constant represents a role name used in authorization attributes and join tables.
    /// </summary>
    public static class Roles
    {
        public const string Admin = nameof(Admin);
        public const string TenantAdmin = nameof(TenantAdmin);
        public const string User = nameof(User);
    }
}
