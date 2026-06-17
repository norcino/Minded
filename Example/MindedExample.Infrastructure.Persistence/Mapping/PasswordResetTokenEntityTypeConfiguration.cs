using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MindedExample.Infrastructure.Persistence.Mapping
{
    /// <summary>
    /// Entity Framework configuration for PasswordResetToken entity.
    /// </summary>
    public class PasswordResetTokenEntityTypeConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Token)
                .IsRequired()
                .HasColumnType("varchar(200)");

            builder.HasIndex(t => t.Token)
                .IsUnique();

            builder.HasOne(t => t.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PasswordResetTokens_Users");
        }
    }
}
