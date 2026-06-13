using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MindedExample.Infrastructure.Persistence.Mapping
{
    public class TransactionEntityTypeConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.CategoryId).HasDefaultValueSql("0");
            builder.Property(c => c.UserId);
            // Precision instead of provider-specific column types: money and datetime
            // are SQL Server-only and do not exist in PostgreSQL. Each provider maps
            // decimal/DateTime to its native equivalent.
            builder.Property(c => c.Credit).HasPrecision(18, 2);
            builder.Property(c => c.Debit).HasPrecision(18, 2);
            builder.Property(c => c.Description).HasColumnType("varchar(500)");
            builder.HasOne(d => d.Category)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Transaction_Categories");
            builder.HasOne(d => d.User)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Transactions_Users");
        }
    }
}
