using System.Collections.Generic;

namespace Data.Entity
{
    public class User
    {
        public User()
        {
            Transactions = new HashSet<Transaction>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
