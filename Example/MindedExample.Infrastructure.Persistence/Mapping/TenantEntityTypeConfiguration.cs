using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MindedExample.Infrastructure.Persistence.Mapping
{
    /// <summary>
    /// Entity Framework configuration for Tenant entity.
    /// </summary>
    public class TenantEntityTypeConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasColumnType("varchar(200)");

            builder.Property(t => t.LegalOwnerUserId)
                .IsRequired(false);

            builder.HasIndex(t => t.Name)
                .HasDatabaseName("IX_Tenants_Name");

            builder.HasIndex(t => t.LegalOwnerUserId)
                .HasDatabaseName("IX_Tenants_LegalOwnerUserId");
        }
    }
}
