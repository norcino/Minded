using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Query.Trait;
using Minded.Framework.CQRS.Tests.TestSupportClasses;
using Minded.Framework.CQRS.Query;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using AnonymousData;
using Minded.Extensions.CQRS.EntityFrameworkCore.Tests.TestSupportClasses;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Collections;

namespace Minded.Framework.CQRS.Tests
{
    [TestClass]
    public class QueryExtensionsTests
    {
        TestDbContext _context;
        [TestInitialize]
        public void TestInitialize() {
            _context = new TestDbCreator().CreateContext();
        }

        [TestCleanup]
        public void TestCleanup() {
            _context.Dispose();
        }

        #region OrderBy
        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Ascending_on_single_property()
        {
            var vehicleQuery = new VehicleQuery();

            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model))
            };

            var vehicles = Builder<Vehicle>.New().BuildMany(1000, (v, i) => { v.Id = 0; });
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();
                        
            var queryResult = vehicleQuery.ApplyTo(_context.Vehicles).ToList();
            queryResult.Should().BeInAscendingOrder(v => v.Model, Comparer<string>.Default);
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Ascending_on_multiple_properties()
        {
            var primaryName = Any.String();
            var people = Builder<Person>.New().BuildMany(1000, (p, i) => {
                p.Id = 0;
                p.Name = i < 800 ? primaryName : Any.String();
                p.Surname = $"{i % 10}";
            }).AsQueryable();

            var peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Person.Name)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Surname))
            };

            _context.People.AddRange(people);
            _context.SaveChanges();

            var queryResult = peopleQuery.ApplyTo(_context.People).ToList();

            queryResult.Should()
                .BeInAscendingOrder(v => v.Name, Comparer<string>.Default)
                .And
                .ThenBeInAscendingOrder(v => v.Surname, Comparer<string>.Default);
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Descending_on_single_property()
        {
            var vehicleQuery = new VehicleQuery();

            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Vehicle.Id))
            };

            var vehicles = Builder<Vehicle>.New().BuildMany(1000, (v, i) => { v.Id = 0; v.Model = Any.String(); });
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            var q = vehicleQuery.ApplyTo(_context.Vehicles);

            var log = ((EntityQueryable<Vehicle>)q).DebugView.Query;
            log.Should().EndWith("ORDER BY [v].[Id] DESC");
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Descending_on_multiple_properties()
        {
            var primaryName = Any.String();
            var people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = 0;
            }).AsQueryable();

            var peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Person.Name)),
                new OrderDescriptor(Order.Descending, nameof(Person.Surname))
            };

            _context.People.AddRange(people);
            _context.SaveChanges();

            var q = peopleQuery.ApplyTo(_context.People);
            var log = ((EntityQueryable<Person>)q).DebugView.Query;
            log.Should().EndWith("ORDER BY [p].[Name] DESC, [p].[Surname] DESC");
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_mixing_in_correct_order_Ascending_and_Descending()
        {
            var primaryName = Any.String();
            var people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = 0;
            }).AsQueryable();

            var peopleQuery = new PeopleQuery();

            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Person.Name)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Surname)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Id))
            };

            _context.People.AddRange(people);
            _context.SaveChanges();

            var q = peopleQuery.ApplyTo(_context.People);

            var log = ((EntityQueryable<Person>)q).DebugView.Query;
            log.Should().EndWith("ORDER BY [p].[Name] DESC, [p].[Surname], [p].[Id]");
        }
        #endregion

        //#region Expand
        [TestMethod]
        public void ApplyTo_Should_expand_single_entity()
        {
            var peopleWithVehicles = Any.String();
            var people = Builder<Person>.New().BuildMany(10, (p, i) =>
            {
                p.Id = 0;
                p.Surname = peopleWithVehicles;
                p.Vehicles = Builder<Vehicle>.New().BuildMany(10, (v, i) =>
                {
                    v.Id = 0;
                    v.Owner = Builder<Person>.New().Build(p2 => p2.Id = 0);
                    v.Maker = Builder<Corporation>.New().Build(c => {
                        c.Id = 0;
                        c.CEO = Builder<Person>.New().Build(p3 => p3.Id = 0);
                    });
                });
            });

            _context.People.AddRange(people);
            _context.SaveChanges();

            var peopleQuery = new PeopleQuery();

            (peopleQuery as ICanExpand).Expand = new[] { nameof(Person.Vehicles) };

            var query = peopleQuery.ApplyTo(_context.People);
            var queryResult = query.ToList().Where(p => p.Surname == peopleWithVehicles);

            queryResult.Should().AllSatisfy(p =>
            {
                p.Vehicles.Should().NotBeEmpty();
                p.Vehicles.Should().AllSatisfy(v => v.Owner.Should().BeNull());
            });
        }

        //[TestMethod]
        //public void ApplyTo_Should_not_expand_when_not_required()
        //{
        //    var people = Builder<Person>.New().BuildMany(10, (p, i) => {
        //        p.Vehicles = Builder<Vehicle>.New().BuildMany(10, (v, i) =>
        //        {
        //            v.Owner = Builder<Person>.New().Build();
        //            v.Maker = Builder<Corporation>.New().Build(c => c.CEO = Builder<Person>.New().Build());
        //        });
        //    }).AsQueryable();

        //    IQuery<Person> peopleQuery = new PeopleQuery();

        //    var queryResult = peopleQuery.ApplyTo(people).ToList();

        //    queryResult[0].Vehicles.Should().BeEmpty();
        //}
        //#endregion
    }
}
