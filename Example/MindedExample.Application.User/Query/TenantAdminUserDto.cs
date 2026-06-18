namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// A lightweight user record returned by the tenant admin users query.
    /// Excludes sensitive data such as password hash.
    /// </summary>
    public class TenantAdminUserDto
    {
        /// <summary>Gets or sets the user's database identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the tenant the user belongs to.</summary>
        public int? TenantId { get; set; }

        /// <summary>Gets or sets the user's first name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the user's surname.</summary>
        public string Surname { get; set; }

        /// <summary>Gets or sets the user's email address.</summary>
        public string Email { get; set; }

        /// <summary>Gets or sets the user's role within the tenant.</summary>
        public string TenantRole { get; set; }
    }
}
