namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// A summary record of a pending tenant join request, excluding sensitive data such as the password hash.
    /// </summary>
    public class TenantJoinRequestSummaryDto
    {
        /// <summary>Gets or sets the join request's database identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the tenant the request is directed to.</summary>
        public int TenantId { get; set; }

        /// <summary>Gets or sets the applicant's first name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the applicant's surname.</summary>
        public string Surname { get; set; }

        /// <summary>Gets or sets the applicant's email address.</summary>
        public string Email { get; set; }

        /// <summary>Gets or sets the UTC creation time in ISO 8601 round-trip format.</summary>
        public string CreatedAtUtc { get; set; }
    }
}
