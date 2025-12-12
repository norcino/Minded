using Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Context.Mapping
{
    /// <summary>
    /// Entity Framework configuration for Category entity.
    /// Defines table structure, column types, and relationships.
    /// </summary>
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
            builder.Property(c => c.UserId)
                    .IsRequired();

            builder.HasOne(d => d.User)
                .WithMany(p => p.Categories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categories_Users");

            builder.HasMany(c => c.Transactions).WithOne(c => c.Category);
        }
    }
}
