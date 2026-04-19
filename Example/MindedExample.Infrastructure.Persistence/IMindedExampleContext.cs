using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;

namespace MindedExample.Infrastructure.Persistence
{
    public interface IMindedExampleContext : IDbContext
    {
        DbSet<Category> Categories { get; set; }
        DbSet<Transaction> Transactions { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<T> Set<T>() where T : class, new();
    }
}
