namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    internal class Vehicle
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public Person Owner { get; set; }
        public Corporation maker { get; set; }
    }
}
