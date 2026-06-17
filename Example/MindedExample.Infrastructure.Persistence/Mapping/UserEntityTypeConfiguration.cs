using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MindedExample.Infrastructure.Persistence.Mapping
{
    /// <summary>
    /// Entity Framework configuration for User entity.
    /// Defines table structure, column types, and relationships.
    /// </summary>
    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.Name)
                .IsRequired()
                .HasColumnType("varchar(100)");
            
            builder.Property(u => u.Surname)
                .IsRequired()
                .HasColumnType("varchar(100)");
            
            builder.Property(u => u.Email)
                .IsRequired()
                .HasColumnType("varchar(250)");

            builder.Property(u => u.TenantRole)
                .IsRequired()
                .HasColumnType("varchar(20)");

            builder.Property(u => u.PasswordHash)
                .HasColumnType("varchar(500)");

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.IsGlobalAdmin)
                .HasDefaultValue(false);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.HasIndex(u => new { u.TenantId, u.Email })
                .HasDatabaseName("IX_Users_Tenant_Email");

            builder.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Users_Tenants");

            builder.HasMany(u => u.OwnedTenants)
                .WithOne(t => t.LegalOwnerUser)
                .HasForeignKey(t => t.LegalOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Tenants_LegalOwnerUser");
            
            builder.HasMany(u => u.Categories)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(u => u.Transactions)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.CreatedInvites)
                .WithOne(i => i.CreatedByUser)
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.PasswordResetTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

