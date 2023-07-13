using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    internal class PersonEntityTypeConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name);
            builder.Property(c => c.Surname);
            builder.HasMany(c => c.Vehicles).WithOne(c => c.Owner);
        }
    }

    internal class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
