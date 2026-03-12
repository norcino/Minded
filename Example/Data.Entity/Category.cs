using System.Collections.Generic;

namespace Data.Entity
{
    /// <summary>
    /// Represents a category for organizing transactions.
    /// Each category belongs to a user and can have multiple transactions.
    /// Supports hierarchical structure with parent-child relationships.
    /// </summary>
    public class Category
    {
        public Category()
        {
            Transactions = new HashSet<Transaction>();
            Children = new HashSet<Category>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public int UserId { get; set; }
        public int? ParentId { get; set; }

        public virtual User User { get; set; }
        public virtual Category Parent { get; set; }
        public virtual ICollection<Category> Children { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
