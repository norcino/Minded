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
using System;

namespace Minded.Framework.CQRS.Tests
{
    [TestClass]
    public class QueryExtensionsTests
    {
        TestDbContext _context;

        /// <summary>
        /// Initializes the test context before each test.
        /// Clears all data from the database to ensure test isolation.
        /// </summary>
        [TestInitialize]
        public void TestInitialize() {
            _context = new TestDbCreator().CreateContext();

            // Clear all data to ensure test isolation
            // Order matters due to foreign key constraints
            _context.Vehicles.RemoveRange(_context.Vehicles);
            _context.People.RemoveRange(_context.People);
            _context.Corporations.RemoveRange(_context.Corporations);
            _context.SaveChanges();
        }

        [TestCleanup]
        public void TestCleanup() {
            _context.Dispose();
        }

        #region OrderBy
        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Ascending_on_single_property()
        {
            // Arrange: Create a query with ascending order by Model property
            var vehicleQuery = new VehicleQuery();
            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model))
            };

            // Arrange: Create test data with vehicles with specific models for predictable ordering
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "Zebra" },
                new Vehicle { Id = 0, Model = "Apple" },
                new Vehicle { Id = 0, Model = "Mango" },
                new Vehicle { Id = 0, Model = "Banana" },
                new Vehicle { Id = 0, Model = "Cherry" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Act: Apply the query to get ordered results
            var query = vehicleQuery.ApplyQueryTo(_context.Vehicles);

            // Verify the query contains the ORDER BY clause
            // This ensures the ApplyQueryTo method correctly applies the ordering trait
#pragma warning disable EF1001 // Internal EF Core API usage.
            var sqlQuery = ((EntityQueryable<Vehicle>)query).DebugView.Query;
#pragma warning restore EF1001 // Internal EF Core API usage.

            var hasSqlServerSyntax = sqlQuery.Contains("ORDER BY [v].[Model]");
            var hasSqliteSyntax = sqlQuery.Contains("ORDER BY \"v\".\"Model\"");
            (hasSqlServerSyntax || hasSqliteSyntax).Should().BeTrue(
                because: "the query should contain ORDER BY for Model column");

            var queryResult = query.ToList();

            // Assert: Verify the results are ordered by Model in ascending order
            // Expected order: Apple, Banana, Cherry, Mango, Zebra
            queryResult.Should().HaveCount(5);
            queryResult[0].Model.Should().Be("Apple", because: "first item should be Apple");
            queryResult[1].Model.Should().Be("Banana", because: "second item should be Banana");
            queryResult[2].Model.Should().Be("Cherry", because: "third item should be Cherry");
            queryResult[3].Model.Should().Be("Mango", because: "fourth item should be Mango");
            queryResult[4].Model.Should().Be("Zebra", because: "fifth item should be Zebra");
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Ascending_on_multiple_properties()
        {
            // Arrange: Create test data with specific names and surnames for predictable ordering
            List<Person> people = new List<Person>
            {
                new Person { Id = 0, Name = "Zebra", Surname = "Z" },
                new Person { Id = 0, Name = "Apple", Surname = "B" },
                new Person { Id = 0, Name = "Apple", Surname = "A" },
                new Person { Id = 0, Name = "Mango", Surname = "M" },
                new Person { Id = 0, Name = "Apple", Surname = "C" }
            };

            // Arrange: Create a query with ascending order by Name, then by Surname
            var peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Person.Name)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Surname))
            };

            _context.People.AddRange(people);
            _context.SaveChanges();

            // Act: Apply the query to get ordered results
            var query = peopleQuery.ApplyQueryTo(_context.People);

            // Verify the query contains the ORDER BY clause for both Name and Surname
            // This ensures the ApplyQueryTo method correctly applies the ordering trait
#pragma warning disable EF1001 // Internal EF Core API usage.
            var sqlQuery = ((EntityQueryable<Person>)query).DebugView.Query;
#pragma warning restore EF1001 // Internal EF Core API usage.

            var hasSqlServerSyntax = sqlQuery.Contains("ORDER BY [p].[Name]") && sqlQuery.Contains("[p].[Surname]");
            var hasSqliteSyntax = sqlQuery.Contains("ORDER BY \"p\".\"Name\"") && sqlQuery.Contains("\"p\".\"Surname\"");
            (hasSqlServerSyntax || hasSqliteSyntax).Should().BeTrue(
                because: "the query should contain ORDER BY for Name and Surname columns");

            var queryResult = query.ToList();

            // Assert: Verify results are ordered first by Name, then by Surname
            // Expected order: Apple (A), Apple (B), Apple (C), Mango (M), Zebra (Z)
            queryResult.Should().HaveCount(5);
            queryResult[0].Name.Should().Be("Apple");
            queryResult[0].Surname.Should().Be("A", because: "first Apple should have surname A");
            queryResult[1].Name.Should().Be("Apple");
            queryResult[1].Surname.Should().Be("B", because: "second Apple should have surname B");
            queryResult[2].Name.Should().Be("Apple");
            queryResult[2].Surname.Should().Be("C", because: "third Apple should have surname C");
            queryResult[3].Name.Should().Be("Mango");
            queryResult[4].Name.Should().Be("Zebra");
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Descending_on_single_property()
        {
            // Arrange: Create a query with descending order by Id property
            var vehicleQuery = new VehicleQuery();
            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Vehicle.Id))
            };

            // Arrange: Create test data with 1000 vehicles
            List<Vehicle> vehicles = Builder<Vehicle>.New().BuildMany(1000, (v, i) => { v.Id = 0; v.Model = Any.String(); });
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Act: Apply the query to get ordered results
            IQueryable<Vehicle> q = vehicleQuery.ApplyQueryTo(_context.Vehicles);

            // Assert: Verify the generated SQL contains the ORDER BY clause with DESC
            // Note: We check for both SQL Server syntax ([v].[Id]) and SQLite syntax ("v"."Id")
            // because different database providers use different identifier quoting styles
#pragma warning disable EF1001 // Internal EF Core API usage.
            var log = ((EntityQueryable<Vehicle>)q).DebugView.Query;
#pragma warning restore EF1001 // Internal EF Core API usage.

            // Check for SQL Server syntax: ORDER BY [v].[Id] DESC
            // OR SQLite syntax: ORDER BY "v"."Id" DESC
            var hasSqlServerSyntax = log.Contains("ORDER BY [v].[Id] DESC");
            var hasSqliteSyntax = log.Contains("ORDER BY \"v\".\"Id\" DESC");

            (hasSqlServerSyntax || hasSqliteSyntax).Should().BeTrue(
                because: "the query should contain ORDER BY with DESC for the Id column, " +
                         "using either SQL Server syntax ([v].[Id]) or SQLite syntax (\"v\".\"Id\")");
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_Descending_on_multiple_properties()
        {
            // Arrange: Create test data with controlled names and surnames
            var primaryName = Any.String();
            IQueryable<Person> people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = 0;
            }).AsQueryable();

            // Arrange: Create a query with descending order by Name, then by Surname
            var peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Person.Name)),
                new OrderDescriptor(Order.Descending, nameof(Person.Surname))
            };

            _context.People.AddRange(people);
            _context.SaveChanges();

            // Act: Apply the query to get ordered results
            IQueryable<Person> q = peopleQuery.ApplyQueryTo(_context.People);

            // Assert: Verify the generated SQL contains the ORDER BY clause with DESC for both columns
            // Note: We check for both SQL Server syntax ([p].[Name]) and SQLite syntax ("p"."Name")
            // because different database providers use different identifier quoting styles
#pragma warning disable EF1001 // Internal EF Core API usage.
            var log = ((EntityQueryable<Person>)q).DebugView.Query;
#pragma warning restore EF1001 // Internal EF Core API usage.

            // Check for SQL Server syntax: ORDER BY [p].[Name] DESC, [p].[Surname] DESC
            // OR SQLite syntax: ORDER BY "p"."Name" DESC, "p"."Surname" DESC
            var hasSqlServerSyntax = log.Contains("ORDER BY [p].[Name] DESC, [p].[Surname] DESC");
            var hasSqliteSyntax = log.Contains("ORDER BY \"p\".\"Name\" DESC, \"p\".\"Surname\" DESC");

            (hasSqlServerSyntax || hasSqliteSyntax).Should().BeTrue(
                because: "the query should contain ORDER BY with DESC for Name and Surname columns, " +
                         "using either SQL Server syntax ([p].[Name]) or SQLite syntax (\"p\".\"Name\")");
        }

        [TestMethod]
        public void ApplyTo_Should_correctly_Order_by_mixing_in_correct_order_Ascending_and_Descending()
        {
            // Arrange: Create test data with controlled names and surnames
            var primaryName = Any.String();
            IQueryable<Person> people = Builder<Person>.New().BuildMany(1000, (v, i) => {
                v.Name = i < 800 ? primaryName : Any.String();
                v.Surname = $"{i % 10}";
                v.Id = 0;
            }).AsQueryable();

            // Arrange: Create a query with mixed ordering:
            // - Name: Descending
            // - Surname: Ascending
            // - Id: Ascending
            var peopleQuery = new PeopleQuery();
            (peopleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Descending, nameof(Person.Name)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Surname)),
                new OrderDescriptor(Order.Ascending, nameof(Person.Id))
            };

            _context.People.AddRange(people);
            _context.SaveChanges();

            // Act: Apply the query to get ordered results
            IQueryable<Person> q = peopleQuery.ApplyQueryTo(_context.People);

            // Assert: Verify the generated SQL contains the correct ORDER BY clause with mixed directions
            // Note: We check for both SQL Server syntax ([p].[Name]) and SQLite syntax ("p"."Name")
            // because different database providers use different identifier quoting styles
#pragma warning disable EF1001 // Internal EF Core API usage.
            var log = ((EntityQueryable<Person>)q).DebugView.Query;
#pragma warning restore EF1001 // Internal EF Core API usage.

            // Check for SQL Server syntax: ORDER BY [p].[Name] DESC, [p].[Surname], [p].[Id]
            // OR SQLite syntax: ORDER BY "p"."Name" DESC, "p"."Surname", "p"."Id"
            var hasSqlServerSyntax = log.Contains("ORDER BY [p].[Name] DESC, [p].[Surname], [p].[Id]");
            var hasSqliteSyntax = log.Contains("ORDER BY \"p\".\"Name\" DESC, \"p\".\"Surname\", \"p\".\"Id\"");

            (hasSqlServerSyntax || hasSqliteSyntax).Should().BeTrue(
                because: "the query should contain ORDER BY with DESC for Name and ASC for Surname and Id, " +
                         "using either SQL Server syntax ([p].[Name]) or SQLite syntax (\"p\".\"Name\")");
        }
        #endregion

        #region Skip
        /// <summary>
        /// Tests that ApplyQueryTo correctly applies the Skip trait to skip a specified number of records.
        /// This is essential for pagination scenarios where we need to skip past already-viewed records.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Skip_specified_number_of_records()
        {
            // Arrange: Create 10 vehicles with sequential IDs for predictable ordering
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 10; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query that orders by Model and skips the first 5 records
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Skip = 5,
                Top = 100 // Explicit top to avoid default 100 limit affecting our test
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return 5 records (10 total - 5 skipped)
            queryResult.Should().HaveCount(5);
            // First record should be Model_06 (after skipping Model_01 through Model_05)
            queryResult[0].Model.Should().Be("Model_06", because: "first 5 records were skipped");
            queryResult[4].Model.Should().Be("Model_10", because: "last record should be Model_10");
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly handles Skip = 0, which should not skip any records.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Skip_zero_records_when_Skip_is_zero()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "Alpha" },
                new Vehicle { Id = 0, Model = "Beta" },
                new Vehicle { Id = 0, Model = "Gamma" },
                new Vehicle { Id = 0, Model = "Delta" },
                new Vehicle { Id = 0, Model = "Epsilon" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Skip = 0
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Skip = 0,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return all 5 records since Skip = 0 doesn't skip anything
            queryResult.Should().HaveCount(5);
            queryResult[0].Model.Should().Be("Alpha", because: "no records were skipped");
        }

        /// <summary>
        /// Tests that ApplyQueryTo does not apply Skip when Skip is null.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_not_Skip_when_Skip_is_null()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "One" },
                new Vehicle { Id = 0, Model = "Two" },
                new Vehicle { Id = 0, Model = "Three" },
                new Vehicle { Id = 0, Model = "Four" },
                new Vehicle { Id = 0, Model = "Five" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Skip = null (default)
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Skip = null,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return all 5 records since Skip is null
            queryResult.Should().HaveCount(5);
        }

        /// <summary>
        /// Tests that Skip works correctly when combined with OrderBy.
        /// The skip should be applied after the ordering.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Skip_after_OrderBy_is_applied()
        {
            // Arrange: Create vehicles with specific models (inserted in random order)
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "Zebra" },
                new Vehicle { Id = 0, Model = "Apple" },
                new Vehicle { Id = 0, Model = "Mango" },
                new Vehicle { Id = 0, Model = "Banana" },
                new Vehicle { Id = 0, Model = "Cherry" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query that orders by Model descending and skips 2
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Descending, nameof(Vehicle.Model)) },
                Skip = 2,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: After ordering DESC (Zebra, Mango, Cherry, Banana, Apple) and skipping 2
            // Should get: Cherry, Banana, Apple
            queryResult.Should().HaveCount(3);
            queryResult[0].Model.Should().Be("Cherry", because: "Zebra and Mango were skipped after DESC ordering");
            queryResult[1].Model.Should().Be("Banana");
            queryResult[2].Model.Should().Be("Apple");
        }
        #endregion

        #region Top
        /// <summary>
        /// Tests that ApplyQueryTo correctly applies the Top trait to limit the number of records returned.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Take_specified_number_of_records()
        {
            // Arrange: Create 20 vehicles
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 20; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Top = 5
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Top = 5
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return only 5 records
            queryResult.Should().HaveCount(5);
            queryResult[0].Model.Should().Be("Model_01");
            queryResult[4].Model.Should().Be("Model_05");
        }

        /// <summary>
        /// Tests that ApplyQueryTo applies a default limit of 100 when Top is null.
        /// This is a critical behavior to prevent unbounded queries from returning too many records.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Take_default_100_when_Top_is_null()
        {
            // Arrange: Create 150 vehicles (more than the default 100)
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 150; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D3}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Top = null (default)
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Top = null // Explicitly null to test default behavior
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return exactly 100 records (the default limit)
            queryResult.Should().HaveCount(100, because: "when Top is null, the default limit of 100 is applied");
        }

        /// <summary>
        /// Tests that Top works correctly when combined with Skip for pagination.
        /// This is the standard pagination pattern: Skip(page * pageSize).Take(pageSize).
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Take_combined_with_Skip_for_pagination()
        {
            // Arrange: Create 50 vehicles
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 50; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query for page 3 with page size 10 (Skip 20, Take 10)
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Skip = 20, // Skip first 2 pages
                Top = 10   // Page size = 10
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return records 21-30
            queryResult.Should().HaveCount(10);
            queryResult[0].Model.Should().Be("Model_21", because: "this is the first record of page 3");
            queryResult[9].Model.Should().Be("Model_30", because: "this is the last record of page 3");
        }

        /// <summary>
        /// Tests that Top = 0 returns no records.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Take_zero_records_when_Top_is_zero()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "One" },
                new Vehicle { Id = 0, Model = "Two" },
                new Vehicle { Id = 0, Model = "Three" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Top = 0
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Top = 0
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return 0 records
            queryResult.Should().HaveCount(0);
        }
        #endregion

        #region FilterExpression
        /// <summary>
        /// Tests that ApplyQueryTo correctly applies a simple equality filter expression.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Filter_with_simple_equality_expression()
        {
            // Arrange: Create vehicles with different models
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "Audi" },
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "Mercedes" },
                new Vehicle { Id = 0, Model = "BMW" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with an equality filter
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model == "BMW",
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return only 3 BMW vehicles
            queryResult.Should().HaveCount(3);
            queryResult.Should().AllSatisfy(v => v.Model.Should().Be("BMW"));
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies a comparison filter expression.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Filter_with_comparison_expression()
        {
            // Arrange: Create people with different IDs (we'll use the generated IDs after save)
            List<Person> people = new List<Person>
            {
                new Person { Id = 0, Name = "Person1", Surname = "A" },
                new Person { Id = 0, Name = "Person2", Surname = "B" },
                new Person { Id = 0, Name = "Person3", Surname = "C" },
                new Person { Id = 0, Name = "Person4", Surname = "D" },
                new Person { Id = 0, Name = "Person5", Surname = "E" }
            };
            _context.People.AddRange(people);
            _context.SaveChanges();

            // Get the ID of the 3rd person to use in our filter
            var thirdPersonId = people[2].Id;

            // Arrange: Create a query that filters by Id > thirdPersonId
            var personQuery = new FullFeaturedPersonQuery
            {
                Filter = p => p.Id > thirdPersonId,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = personQuery.ApplyQueryTo(_context.People).ToList();

            // Assert: Should return 2 people (Person4 and Person5)
            queryResult.Should().HaveCount(2);
            queryResult.Should().AllSatisfy(p => p.Id.Should().BeGreaterThan(thirdPersonId));
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies a complex AND filter expression.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Filter_with_complex_and_expression()
        {
            // Arrange: Create people with various names and surnames
            List<Person> people = new List<Person>
            {
                new Person { Id = 0, Name = "John", Surname = "Doe" },
                new Person { Id = 0, Name = "John", Surname = "Smith" },
                new Person { Id = 0, Name = "Jane", Surname = "Doe" },
                new Person { Id = 0, Name = "John", Surname = "Doe" },
                new Person { Id = 0, Name = "Bob", Surname = "Brown" }
            };
            _context.People.AddRange(people);
            _context.SaveChanges();

            // Arrange: Create a query with an AND condition
            var personQuery = new FullFeaturedPersonQuery
            {
                Filter = p => p.Name == "John" && p.Surname == "Doe",
                Top = 100
            };

            // Act: Apply the query
            var queryResult = personQuery.ApplyQueryTo(_context.People).ToList();

            // Assert: Should return only 2 people (John Doe)
            queryResult.Should().HaveCount(2);
            queryResult.Should().AllSatisfy(p =>
            {
                p.Name.Should().Be("John");
                p.Surname.Should().Be("Doe");
            });
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies a complex OR filter expression.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Filter_with_complex_or_expression()
        {
            // Arrange: Create vehicles with different models
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "Audi" },
                new Vehicle { Id = 0, Model = "Mercedes" },
                new Vehicle { Id = 0, Model = "Toyota" },
                new Vehicle { Id = 0, Model = "Honda" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with an OR condition
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model == "BMW" || v.Model == "Audi",
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return 2 vehicles (BMW and Audi)
            queryResult.Should().HaveCount(2);
            queryResult.Should().AllSatisfy(v =>
                (v.Model == "BMW" || v.Model == "Audi").Should().BeTrue());
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies a Contains filter expression.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Filter_with_contains_expression()
        {
            // Arrange: Create vehicles with different models
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "BMW X5" },
                new Vehicle { Id = 0, Model = "BMW M3" },
                new Vehicle { Id = 0, Model = "Audi A4" },
                new Vehicle { Id = 0, Model = "Mercedes E-Class" },
                new Vehicle { Id = 0, Model = "BMW 320i" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with a Contains filter
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model.Contains("BMW"),
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return 3 vehicles containing "BMW"
            queryResult.Should().HaveCount(3);
            queryResult.Should().AllSatisfy(v => v.Model.Should().Contain("BMW"));
        }

        /// <summary>
        /// Tests that ApplyQueryTo does not apply filtering when Filter is null.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_not_Filter_when_Filter_is_null()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "One" },
                new Vehicle { Id = 0, Model = "Two" },
                new Vehicle { Id = 0, Model = "Three" },
                new Vehicle { Id = 0, Model = "Four" },
                new Vehicle { Id = 0, Model = "Five" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Filter = null
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = null,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return all 5 vehicles
            queryResult.Should().HaveCount(5);
        }
        #endregion

        #region Expand
        /// <summary>
        /// Tests that ApplyQueryTo correctly applies the Expand trait to include a single related entity.
        /// Uses EF Core's Include() method to eagerly load navigation properties.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Include_single_related_entity()
        {
            // Arrange: Create a person with vehicles
            var person = new Person { Id = 0, Name = "John", Surname = "Doe" };
            _context.People.Add(person);
            _context.SaveChanges();

            var vehicle = new Vehicle { Id = 0, Model = "BMW", Owner = person };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();

            // Arrange: Create a query that expands the Owner navigation property
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Expand = new[] { nameof(Vehicle.Owner) },
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: The Owner should be loaded (not null)
            queryResult.Should().HaveCount(1);
            queryResult[0].Owner.Should().NotBeNull("because Owner was included via Expand");
            queryResult[0].Owner.Name.Should().Be("John");
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies multiple Expand properties.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Include_multiple_related_entities()
        {
            // Arrange: Create a corporation and person
            var ceo = new Person { Id = 0, Name = "CEO", Surname = "Boss" };
            _context.People.Add(ceo);
            _context.SaveChanges();

            var corporation = new Corporation { Id = 0, Name = "BMW Corp", CEO = ceo };
            _context.Corporations.Add(corporation);

            var owner = new Person { Id = 0, Name = "Owner", Surname = "Person" };
            _context.People.Add(owner);
            _context.SaveChanges();

            var vehicle = new Vehicle { Id = 0, Model = "BMW", Owner = owner, Maker = corporation };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();

            // Arrange: Create a query that expands both Owner and Maker
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Expand = new[] { nameof(Vehicle.Owner), nameof(Vehicle.Maker) },
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Both Owner and Maker should be loaded
            queryResult.Should().HaveCount(1);
            queryResult[0].Owner.Should().NotBeNull("because Owner was included via Expand");
            queryResult[0].Maker.Should().NotBeNull("because Maker was included via Expand");
            queryResult[0].Maker.Name.Should().Be("BMW Corp");
        }

        /// <summary>
        /// Tests that navigation properties are not loaded when Expand is null.
        /// This is the default lazy loading behavior.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_not_Include_when_Expand_is_null()
        {
            // Arrange: Create a person with vehicles
            var person = new Person { Id = 0, Name = "John", Surname = "Doe" };
            _context.People.Add(person);
            _context.SaveChanges();

            var vehicle = new Vehicle { Id = 0, Model = "BMW", Owner = person };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();

            // Detach all entities to ensure clean state
            _context.ChangeTracker.Clear();

            // Arrange: Create a query with Expand = null
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Expand = null,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Owner should NOT be loaded (null) because we didn't include it
            queryResult.Should().HaveCount(1);
            queryResult[0].Owner.Should().BeNull("because Owner was not included via Expand");
        }

        /// <summary>
        /// Tests that navigation properties are not loaded when Expand is an empty array.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_not_Include_when_Expand_is_empty()
        {
            // Arrange: Create a person with vehicles
            var person = new Person { Id = 0, Name = "John", Surname = "Doe" };
            _context.People.Add(person);
            _context.SaveChanges();

            var vehicle = new Vehicle { Id = 0, Model = "BMW", Owner = person };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();

            // Detach all entities to ensure clean state
            _context.ChangeTracker.Clear();

            // Arrange: Create a query with Expand = empty array
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Expand = Array.Empty<string>(),
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Owner should NOT be loaded (null) because Expand is empty
            queryResult.Should().HaveCount(1);
            queryResult[0].Owner.Should().BeNull("because Expand was empty");
        }
        #endregion

        #region Count
        /// <summary>
        /// Tests that ApplyQueryTo correctly sets CountValue when Count is true.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_set_CountValue_when_Count_is_true()
        {
            // Arrange: Create 15 vehicles
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 15; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Count = true
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Count = true,
                Top = 100
            };

            // Act: Apply the query and materialize results
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: CountValue should be set to 15
            vehicleQuery.CountValue.Should().Be(15, because: "there are 15 vehicles in the database");
            queryResult.Should().HaveCount(15, because: "data should also be returned when CountOnly is false");
        }

        /// <summary>
        /// Tests that ApplyQueryTo does not set CountValue when Count is false.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_not_count_when_Count_is_false()
        {
            // Arrange: Create 10 vehicles
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 10; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Count = false
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Count = false,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: CountValue should remain 0 (default) since Count was false
            vehicleQuery.CountValue.Should().Be(0, because: "Count was set to false");
            queryResult.Should().HaveCount(10, because: "data should still be returned");
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly counts records after Filter is applied.
        /// The count should reflect the filtered results, not the total records.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Count_after_Filter_is_applied()
        {
            // Arrange: Create vehicles with different models
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "Audi" },
                new Vehicle { Id = 0, Model = "Audi" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query that filters by BMW and counts
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model == "BMW",
                Count = true,
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: CountValue should be 3 (only BMW vehicles)
            vehicleQuery.CountValue.Should().Be(3, because: "only 3 vehicles match the BMW filter");
            queryResult.Should().HaveCount(3);
        }

        /// <summary>
        /// Tests that Count reflects records after Top is applied.
        /// Note: Based on the implementation, Count is executed AFTER Top,
        /// so it counts the limited result set.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_Count_with_Top_applied()
        {
            // Arrange: Create 20 vehicles
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 20; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with Count and Top = 5
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Count = true,
                Top = 5
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: CountValue should be 5 (after Top is applied)
            // Note: This is the current behavior - count is after Take()
            vehicleQuery.CountValue.Should().Be(5, because: "count is applied after Top limits the results");
            queryResult.Should().HaveCount(5);
        }
        #endregion

        #region Combined Traits
        /// <summary>
        /// Tests that ApplyQueryTo correctly applies OrderBy, Skip, and Top together for pagination.
        /// This is the standard pagination pattern used in most applications.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_apply_OrderBy_Skip_and_Top_for_pagination()
        {
            // Arrange: Create 50 vehicles with sequential model names
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 50; i++)
            {
                vehicles.Add(new Vehicle { Id = 0, Model = $"Model_{i:D2}" });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query for page 2 with page size 10
            // Should return records 11-20 when ordered by Model ascending
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Skip = 10, // Skip first page
                Top = 10   // Page size = 10
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return models 11-20
            queryResult.Should().HaveCount(10);
            queryResult[0].Model.Should().Be("Model_11", because: "this is the first record of page 2");
            queryResult[9].Model.Should().Be("Model_20", because: "this is the last record of page 2");
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies Filter, OrderBy, and Top together.
        /// This is a common scenario: filter data, order it, then take the first N records.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_apply_Filter_OrderBy_and_Top_together()
        {
            // Arrange: Create vehicles with different models
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "BMW X5" },
                new Vehicle { Id = 0, Model = "BMW M3" },
                new Vehicle { Id = 0, Model = "Audi A4" },
                new Vehicle { Id = 0, Model = "BMW 320i" },
                new Vehicle { Id = 0, Model = "BMW Z4" },
                new Vehicle { Id = 0, Model = "Mercedes E-Class" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query that filters for BMW, orders by Model, and takes top 2
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model.Contains("BMW"),
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Top = 2
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return first 2 BMW vehicles when sorted alphabetically
            queryResult.Should().HaveCount(2);
            queryResult[0].Model.Should().Be("BMW 320i", because: "first alphabetically among BMW models");
            queryResult[1].Model.Should().Be("BMW M3", because: "second alphabetically among BMW models");
        }

        /// <summary>
        /// Tests that ApplyQueryTo correctly applies all traits together.
        /// This is the comprehensive test for the full query pipeline.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_apply_all_traits_together()
        {
            // Arrange: Create a person to be the owner
            var owner = new Person { Id = 0, Name = "Owner", Surname = "Person" };
            _context.People.Add(owner);
            _context.SaveChanges();

            // Arrange: Create vehicles with the owner
            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 1; i <= 30; i++)
            {
                vehicles.Add(new Vehicle
                {
                    Id = 0,
                    Model = i % 2 == 0 ? $"BMW_{i:D2}" : $"Audi_{i:D2}",
                    Owner = owner
                });
            }
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a comprehensive query using all traits
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model.Contains("BMW"),           // Filter: only BMW
                OrderBy = new List<OrderDescriptor>
                {
                    new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model))
                },
                Skip = 5,                                         // Skip first 5 BMWs
                Top = 3,                                          // Take 3 records
                Expand = new[] { nameof(Vehicle.Owner) },         // Include Owner
                Count = true                                       // Enable count
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Verify all traits were applied correctly
            // There are 15 BMW vehicles (even numbers 2,4,6,...,30)
            // After sorting, skipping 5, and taking 3:
            queryResult.Should().HaveCount(3, because: "Top = 3");
            queryResult.Should().AllSatisfy(v => v.Model.Should().Contain("BMW"));
            queryResult.Should().AllSatisfy(v => v.Owner.Should().NotBeNull());
            vehicleQuery.CountValue.Should().Be(3, because: "count is applied after all other traits");
        }

        /// <summary>
        /// Tests that traits are applied in the correct order:
        /// OrderBy -> Expand -> Filter -> Skip -> Top -> Count
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_apply_traits_in_correct_order()
        {
            // Arrange: Create vehicles with specific models
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "A_Keep" },
                new Vehicle { Id = 0, Model = "B_Keep" },
                new Vehicle { Id = 0, Model = "C_Keep" },
                new Vehicle { Id = 0, Model = "D_Skip" },
                new Vehicle { Id = 0, Model = "E_Skip" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query that tests trait order
            // Filter for "Keep", order DESC, skip 1, take 1
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model.Contains("Keep"),
                OrderBy = new List<OrderDescriptor>
                {
                    new OrderDescriptor(Order.Descending, nameof(Vehicle.Model))
                },
                Skip = 1,
                Top = 1
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert:
            // 1. Filter: A_Keep, B_Keep, C_Keep (3 records)
            // 2. Order DESC: C_Keep, B_Keep, A_Keep
            // 3. Skip 1: B_Keep, A_Keep
            // 4. Top 1: B_Keep
            queryResult.Should().HaveCount(1);
            queryResult[0].Model.Should().Be("B_Keep",
                because: "after filtering 'Keep', ordering DESC, skipping 1, and taking 1");
        }
        #endregion

        #region Edge Cases
        /// <summary>
        /// Tests that ApplyQueryTo handles an empty OrderBy list correctly (no ordering applied).
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_handle_empty_OrderBy_list()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "Zebra" },
                new Vehicle { Id = 0, Model = "Apple" },
                new Vehicle { Id = 0, Model = "Mango" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with an empty OrderBy list
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor>(), // Empty list
                Top = 100
            };

            // Act: Apply the query (should not throw)
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return all records without any specific ordering
            queryResult.Should().HaveCount(3);
        }

        /// <summary>
        /// Tests that ApplyQueryTo handles null OrderBy correctly.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_handle_null_OrderBy()
        {
            // Arrange: Create 3 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "One" },
                new Vehicle { Id = 0, Model = "Two" },
                new Vehicle { Id = 0, Model = "Three" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with null OrderBy
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = null,
                Top = 100
            };

            // Act: Apply the query (should not throw)
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return all records
            queryResult.Should().HaveCount(3);
        }

        /// <summary>
        /// Tests that ApplyQueryTo returns empty results when Filter matches nothing.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_return_empty_when_Filter_matches_nothing()
        {
            // Arrange: Create 3 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "BMW" },
                new Vehicle { Id = 0, Model = "Audi" },
                new Vehicle { Id = 0, Model = "Mercedes" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with a filter that matches nothing
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Filter = v => v.Model == "NonExistent",
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return empty list
            queryResult.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that ApplyQueryTo handles Skip larger than total records.
        /// Should return empty list when skipping more records than exist.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_return_empty_when_Skip_exceeds_total_records()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "One" },
                new Vehicle { Id = 0, Model = "Two" },
                new Vehicle { Id = 0, Model = "Three" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query that skips more records than exist
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Skip = 100, // Skip 100, but only 3 exist
                Top = 100
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return empty list
            queryResult.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that ApplyQueryTo works correctly with an empty database.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_handle_empty_database()
        {
            // Arrange: No vehicles in the database
            // Create a query with various traits
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                OrderBy = new List<OrderDescriptor> { new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model)) },
                Filter = v => v.Model.Contains("BMW"),
                Skip = 0,
                Top = 10,
                Count = true
            };

            // Act: Apply the query to empty database
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return empty list and count should be 0
            queryResult.Should().BeEmpty();
            vehicleQuery.CountValue.Should().Be(0);
        }

        /// <summary>
        /// Tests that ApplyQueryTo works with a query that implements only ICanOrderBy trait.
        /// Uses the original VehicleQuery class that only has ICanOrderBy.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_work_with_minimal_trait_implementation()
        {
            // Arrange: Create 5 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "Zebra" },
                new Vehicle { Id = 0, Model = "Apple" },
                new Vehicle { Id = 0, Model = "Mango" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Use the original VehicleQuery that only implements ICanOrderBy
            var vehicleQuery = new VehicleQuery();
            (vehicleQuery as ICanOrderBy).OrderBy = new List<OrderDescriptor>
            {
                new OrderDescriptor(Order.Ascending, nameof(Vehicle.Model))
            };

            // Act: Apply the query
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: Should return records ordered by Model
            // Note: Default Top = 100 is still applied even without ICanTop
            queryResult.Should().HaveCount(3);
            queryResult[0].Model.Should().Be("Apple");
            queryResult[1].Model.Should().Be("Mango");
            queryResult[2].Model.Should().Be("Zebra");
        }

        /// <summary>
        /// Tests that ApplyQueryTo handles negative Skip value.
        /// SQLite in-memory treats negative OFFSET as 0 (no skip), returning all records.
        /// This test documents the actual behavior - negative skip values are effectively ignored.
        /// </summary>
        [TestMethod]
        public void ApplyTo_Should_handle_negative_Skip_value()
        {
            // Arrange: Create 3 vehicles
            List<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 0, Model = "One" },
                new Vehicle { Id = 0, Model = "Two" },
                new Vehicle { Id = 0, Model = "Three" }
            };
            _context.Vehicles.AddRange(vehicles);
            _context.SaveChanges();

            // Arrange: Create a query with negative Skip
            var vehicleQuery = new FullFeaturedVehicleQuery
            {
                Skip = -5, // Negative value
                Top = 100
            };

            // Act: Apply the query - SQLite treats negative OFFSET as 0
            var queryResult = vehicleQuery.ApplyQueryTo(_context.Vehicles).ToList();

            // Assert: SQLite in-memory treats negative skip as 0, so all records are returned
            // Note: Different databases may handle this differently (some may throw)
            queryResult.Should().HaveCount(3,
                because: "SQLite treats negative OFFSET as 0, returning all records");
        }
        #endregion
    }
}
