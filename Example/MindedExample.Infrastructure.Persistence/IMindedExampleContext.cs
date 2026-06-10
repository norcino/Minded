using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;

namespace MindedExample.Infrastructure.Persistence
{
    public interface IMindedExampleContext : IDbContext
    {
        DbSet<Category> Categories { get; set; }
        DbSet<Transaction> Transactions { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<Tenant> Tenants { get; set; }
        DbSet<TenantInvite> TenantInvites { get; set; }
        DbSet<TenantJoinRequest> TenantJoinRequests { get; set; }
        DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        DbSet<T> Set<T>() where T : class, new();
    }
}
