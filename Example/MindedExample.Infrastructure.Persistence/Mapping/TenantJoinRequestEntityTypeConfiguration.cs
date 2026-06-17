using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MindedExample.Infrastructure.Persistence.Mapping
{
    /// <summary>
    /// Entity Framework configuration for tenant join requests.
    /// </summary>
    public class TenantJoinRequestEntityTypeConfiguration : IEntityTypeConfiguration<TenantJoinRequest>
    {
        public void Configure(EntityTypeBuilder<TenantJoinRequest> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasColumnType("varchar(100)");

            builder.Property(x => x.Surname)
                .IsRequired()
                .HasColumnType("varchar(100)");

            builder.Property(x => x.Email)
                .IsRequired()
                .HasColumnType("varchar(250)");

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasColumnType("varchar(500)");

            builder.HasIndex(x => new { x.TenantId, x.Email, x.ProcessedAtUtc })
                .HasDatabaseName("IX_TenantJoinRequests_Tenant_Email_Processed");

            builder.HasOne(x => x.Tenant)
                .WithMany(t => t.JoinRequests)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TenantJoinRequests_Tenants");

            builder.HasOne(x => x.ProcessedByUser)
                .WithMany()
                .HasForeignKey(x => x.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TenantJoinRequests_ProcessedByUser");
        }
    }
}
