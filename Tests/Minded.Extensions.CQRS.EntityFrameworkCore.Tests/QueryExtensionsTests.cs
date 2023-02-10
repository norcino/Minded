using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Query.Trait;
using Minded.Framework.CQRS.Tests.TestSupportClasses;
using Minded.Framework.CQRS.Query;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using AnonymousData;

namespace Minded.Framework.CQRS.Tests
{
    [TestClass]
    public class QueryExtensionsTests
    {
        #region OrderBy
        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Ascending_on_single_property()
        {
            IQuery<Vehicle> vehicleQuery = new VehicleQuery();

            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Vehicle.Id))
            };

            IQueryable<Vehicle> vehicles = Builder<Vehicle>.New().BuildMany(1000).AsQueryable();

            var queryResult = vehicleQuery.ApplyTo(vehicles).ToList();
            queryResult.Should().BeInAscendingOrder(v => v.Id);
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Ascending_on_multiple_properties()
        {
            var primaryName = Any.String();
            var people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = i;
            }).AsQueryable();

            IQuery<Person> peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Person.Name)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Surname))
            };

            var queryResult = peopleQuery.ApplyTo(people).ToList();

            for (var i = 0; i < queryResult.Count - 1; i++)
            {
                var nameComparisonValue = queryResult[i].Name.CompareTo(queryResult[i + 1].Name);
                Assert.IsTrue(nameComparisonValue <= 0, "ID must be in descending order");

                // If the same I can test the Surname to be ascending
                if (nameComparisonValue == 0)
                {
                    var surnameComparisonValue = queryResult[i].Surname.CompareTo(queryResult[i + 1].Surname);
                    Assert.IsTrue(surnameComparisonValue <= 0, "ID must be in ascending order");
                }
            }
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Descending_on_single_property()
        {
            IQuery<Vehicle> vehicleQuery = new VehicleQuery();

            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Vehicle.Id))
            };

            IQueryable<Vehicle> vehicles = Builder<Vehicle>.New().BuildMany(1000).AsQueryable();

            var queryResult = vehicleQuery.ApplyTo(vehicles).ToList();

            queryResult.Should().BeInDescendingOrder(v => v.Id);
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Descending_on_multiple_properties()
        {
            var primaryName = Any.String();
            var people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = i;
            }).AsQueryable();

            IQuery<Person> peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Person.Name)),
                new OrderDescriptor(Order.Descending, nameof(Person.Surname))
            };

            var queryResult = peopleQuery.ApplyTo(people).ToList();

            for (var i = 0; i < queryResult.Count - 1; i++)
            {
                var nameComparisonValue = queryResult[i].Name.CompareTo(queryResult[i + 1].Name);
                Assert.IsTrue(nameComparisonValue >= 0, "ID must be in descending order");

                // If the same I can test the Surname to be ascending
                if (nameComparisonValue == 0)
                {
                    var surnameComparisonValue = queryResult[i].Surname.CompareTo(queryResult[i + 1].Surname);
                    Assert.IsTrue(surnameComparisonValue >= 0, "ID must be in ascending order");
                }
            }
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_mixing_in_correct_order_Ascending_and_Descending()
        {
            var primaryName = Any.String();
            var people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = i;
            }).AsQueryable();

            IQuery<Person> peopleQuery = new PeopleQuery();

            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Person.Name)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Surname)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Id))
            };

            var queryResult = peopleQuery.ApplyTo(people).ToList();

            for (var i = 0; i < queryResult.Count - 1; i++)
            {
                var nameComparisonValue = queryResult[i].Name.CompareTo(queryResult[i + 1].Name);
                Assert.IsTrue(nameComparisonValue >= 0, "ID must be in descending order");

                // If the same I can test the Surname to be ascending
                if (nameComparisonValue == 0)
                {
                    var surnameComparisonValue = queryResult[i].Surname.CompareTo(queryResult[i + 1].Surname);
                    Assert.IsTrue(surnameComparisonValue <= 0, "ID must be in ascending order");

                    // If the same I can test the ID to be ascending
                    if (surnameComparisonValue == 0)
                    {
                        var idComparisonValue = queryResult[i].Id.CompareTo(queryResult[i + 1].Id);
                        Assert.IsTrue(idComparisonValue < 0, "ID must be in ascending order");
                    }
                }
            }
        }
        #endregion

        #region Expand
        [TestMethod]
        public void ApplyTo_Should_expand_single_entity()
        {
            var people = Builder<Person>.New().BuildMany(10, (p, i) => {
                p.Vehicles = Builder<Vehicle>.New().BuildMany(10, (v, i) =>
                {
                    v.Owner = Builder<Person>.New().Build();
                    v.Maker = Builder<Corporation>.New().Build(c => c.CEO = Builder<Person>.New().Build());
                });
            }).AsQueryable();

            IQuery<Person> peopleQuery = new PeopleQuery();

            (peopleQuery as ICanExpand).Expand = new[] { nameof(Person.Vehicles) };

            var queryResult = peopleQuery.ApplyTo(people).ToList();

            queryResult[0].Vehicles.Should().NotBeEmpty();
        }

        [TestMethod]
        public void ApplyTo_Should_not_expand_when_not_required()
        {
            var people = Builder<Person>.New().BuildMany(10, (p, i) => {
                p.Vehicles = Builder<Vehicle>.New().BuildMany(10, (v, i) =>
                {
                    v.Owner = Builder<Person>.New().Build();
                    v.Maker = Builder<Corporation>.New().Build(c => c.CEO = Builder<Person>.New().Build());
                });
            }).AsQueryable();

            IQuery<Person> peopleQuery = new PeopleQuery();

            var queryResult = peopleQuery.ApplyTo(people).ToList();

            queryResult[0].Vehicles.Should().BeEmpty();
        }
        #endregion
    }
}
