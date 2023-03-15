using Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Context.Mapping
{
    public class TransactionEntityTypeConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.CategoryId).HasDefaultValueSql("0");
            builder.Property(c => c.UserId);
            builder.Property(c => c.Credit).HasColumnType("money");
            builder.Property(c => c.Debit).HasColumnType("money");
            builder.Property(c => c.Description).HasColumnType("varchar(500)");
            builder.Property(c => c.Recorded).HasColumnType("datetime");
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
