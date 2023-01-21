using Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Context.Mapping
{
    public class CategoryEntityTypeConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Description)
                    .IsRequired()
                    .HasColumnType("varchar(500)");
            builder.Property(c => c.Name)
                    .IsRequired()
                    .HasColumnType("varchar(250)");
            builder.HasMany(c => c.Transactions).WithOne(c => c.Category);
        }
    }
}
