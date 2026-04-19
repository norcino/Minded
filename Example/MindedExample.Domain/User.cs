using System.Collections.Generic;
using Minded.Extensions.DataProtection.Abstractions;

namespace MindedExample.Domain
{
    /// <summary>
    /// Represents a user in the system.
    /// Contains personal information including sensitive data (name, surname, and email) that is protected in logs.
    /// </summary>
    public class User
    {
        public User()
        {
            Transactions = new HashSet<Transaction>();
            Categories = new HashSet<Category>();
            Roles = new HashSet<string>();
        }

        public int Id { get; set; }

        /// <summary>
        /// User's first name. Marked as sensitive data - will be omitted from logs by default.
        /// </summary>
        [SensitiveData]
        public string Name { get; set; }

        /// <summary>
        /// User's surname. Marked as sensitive data - will be omitted from logs by default.
        /// </summary>
        [SensitiveData]
        public string Surname { get; set; }

        /// <summary>
        /// User's email address. Marked as sensitive data - will be omitted from logs by default.
        /// </summary>
        [SensitiveData]
        public string Email { get; set; }

        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }

        /// <summary>
        /// Role names assigned to this user (stored in UserRoles join table).
        /// </summary>
        public virtual ICollection<string> Roles { get; set; }
    }
}