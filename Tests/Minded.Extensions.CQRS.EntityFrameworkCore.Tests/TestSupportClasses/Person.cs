using System.Collections.Generic;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    internal class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
