namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    internal class Corporation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Person CEO { get; set; }
    }
}
