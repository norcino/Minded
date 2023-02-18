using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    internal class VehicleEntityTypeConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Model)
                    .IsRequired();
            builder.HasOne(c => c.Maker);
            builder.HasOne(c => c.Owner).WithMany(c => c.Vehicles);
        }
    }

    internal class Vehicle
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public Person Owner { get; set; }
        public Corporation Maker { get; set; }
    }
}
