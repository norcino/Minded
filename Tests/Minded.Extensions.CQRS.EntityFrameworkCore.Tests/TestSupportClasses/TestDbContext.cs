using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Tests.TestSupportClasses;

namespace Minded.Extensions.CQRS.EntityFrameworkCore.Tests.TestSupportClasses
{
    public class TestDbContext : DbContext
    {
        private readonly Action<TestDbContext, ModelBuilder> _modelCustomizer;

        #region Constructors
        public TestDbContext()
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options,
            Action<TestDbContext, ModelBuilder> modelCustomizer = null)
            : base(options)
        {
            _modelCustomizer = modelCustomizer;
        }
        #endregion

        internal DbSet<Vehicle> Vehicles => Set<Vehicle>();
        internal DbSet<Person> People => Set<Person>();
        internal DbSet<Corporation> Corporations => Set<Corporation>();

        #region OnConfiguring
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                   .EnableSensitiveDataLogging();

                optionsBuilder.UseSqlServer(
                    @"Server=(localdb)\mssqllocaldb;Database=TestDB;Trusted_Connection=True");
            }
        }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (_modelCustomizer is not null)
            {
                _modelCustomizer(this, modelBuilder);
            }
        }
    }
}
