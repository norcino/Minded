using System.Collections.Generic;
using Minded.Extensions.DataProtection.Abstractions;

namespace Data.Entity
{
    /// <summary>
    /// Represents a user in the system.
    /// Contains personal information including sensitive data (email and surname) that is protected in logs.
    /// </summary>
    public class User
    {
        public User()
        {
            Transactions = new HashSet<Transaction>();
        }

        public int Id { get; set; }
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

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
