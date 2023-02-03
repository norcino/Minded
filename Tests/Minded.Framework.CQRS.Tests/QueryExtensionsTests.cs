using Minded.Framework.CQRS.Query.Trait;
using Minded.Framework.CQRS.Tests.TestSupportClasses;
using Minded.Framework.CQRS.Query;

namespace Minded.Framework.CQRS.Tests
{
    [TestClass]
    public class QueryExtensionsTests
    {
        [TestMethod]
        public void ApplyTo_Supports_Order_by_Ascending_on_single_property()
        {
            IQuery<Vehicle> vehicleQuery = new VehicleQuery();

            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Vehicle.Id))
            };

            IQueryable<Vehicle> vehicles = Builder<Vehicle>.New().BuildMany(10).AsQueryable();

            var queryResult = vehicleQuery.ApplyToBanana<Vehicle>(vehicles).ToList();
        }
    }
}
