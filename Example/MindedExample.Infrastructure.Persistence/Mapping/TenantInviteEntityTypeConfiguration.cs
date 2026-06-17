using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MindedExample.Infrastructure.Persistence.Mapping
{
    /// <summary>
    /// Entity Framework configuration for TenantInvite entity.
    /// </summary>
    public class TenantInviteEntityTypeConfiguration : IEntityTypeConfiguration<TenantInvite>
    {
        public void Configure(EntityTypeBuilder<TenantInvite> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Email)
                .HasColumnType("varchar(250)");

            builder.Property(i => i.Code)
                .IsRequired()
                .HasColumnType("varchar(32)");

            builder.Property(i => i.Token)
                .IsRequired()
                .HasColumnType("varchar(200)");

            builder.HasIndex(i => i.Code)
                .IsUnique();

            builder.HasIndex(i => i.Token)
                .IsUnique();

            builder.HasOne(i => i.Tenant)
                .WithMany(t => t.Invites)
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TenantInvites_Tenants");

            builder.HasOne(i => i.CreatedByUser)
                .WithMany(u => u.CreatedInvites)
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TenantInvites_CreatedByUser");

            builder.HasOne(i => i.UsedByUser)
                .WithMany()
                .HasForeignKey(i => i.UsedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TenantInvites_UsedByUser");
        }
    }
}
