using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Minded.Extensions.CQRS.EntityFrameworkCore.Tests.TestSupportClasses
{
    public class TestDbCreator
    {
        private const string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=TestDB;Trusted_Connection=True";

        private static readonly object _lock = new();
        private static bool _databaseInitialized;

        public TestDbCreator()
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    using (var context = CreateContext())
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                    }

                    _databaseInitialized = true;
                }
            }
        }

        public TestDbContext CreateContext()
            => new TestDbContext(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlServer(ConnectionString)
                    .Options);
    }
}
