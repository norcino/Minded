using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    internal class CorporationEntityTypeConfiguration : IEntityTypeConfiguration<Corporation>
    {
        public void Configure(EntityTypeBuilder<Corporation> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name);
            builder.HasOne(c => c.CEO);
        }
    }

    internal class Corporation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Person CEO { get; set; }
    }
}
