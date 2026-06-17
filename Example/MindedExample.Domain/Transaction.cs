using System;
using Minded.Extensions.DataProtection.Abstractions;

namespace MindedExample.Domain
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Recorded { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        [SensitiveData]
        public string Description { get; set; }
        public int CategoryId { get; set; }

        public int UserId { get; set; }

        public virtual Category Category { get; set; }
        public virtual User User { get; set; }
    }
}
