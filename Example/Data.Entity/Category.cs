using System.Collections.Generic;

namespace Data.Entity
{
    public class Category
    {
        public Category()
        {
            Transactions = new HashSet<Transaction>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
