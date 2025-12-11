using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Tests.TestSupportClasses;

namespace Minded.Extensions.CQRS.EntityFrameworkCore.Tests.TestSupportClasses
{
    /// <summary>
    /// Test database context for EntityFrameworkCore extension tests.
    /// Supports multiple database providers configured via TestDbCreator.
    /// The database provider and connection string are determined at runtime based on configuration.
    /// </summary>
    public class TestDbContext : DbContext
    {
        private readonly Action<TestDbContext, ModelBuilder> _modelCustomizer;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of TestDbContext.
        /// Note: This constructor should not be used directly. Use TestDbCreator.CreateContext() instead.
        /// </summary>
        public TestDbContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of TestDbContext with the specified options.
        /// </summary>
        /// <param name="options">The DbContext options configured by TestDbCreator</param>
        /// <param name="modelCustomizer">Optional action to customize the model configuration</param>
        public TestDbContext(DbContextOptions<TestDbContext> options,
            Action<TestDbContext, ModelBuilder> modelCustomizer = null)
            : base(options)
        {
            _modelCustomizer = modelCustomizer;
        }
        #endregion

        /// <summary>
        /// Gets the Vehicles DbSet for testing vehicle-related queries
        /// </summary>
        internal DbSet<Vehicle> Vehicles => Set<Vehicle>();

        /// <summary>
        /// Gets the People DbSet for testing person-related queries
        /// </summary>
        internal DbSet<Person> People => Set<Person>();

        /// <summary>
        /// Gets the Corporations DbSet for testing corporation-related queries
        /// </summary>
        internal DbSet<Corporation> Corporations => Set<Corporation>();

        /// <summary>
        /// Configures the model for the context.
        /// Applies any custom model configuration provided via the constructor.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (_modelCustomizer is not null)
            {
                _modelCustomizer(this, modelBuilder);
            }
        }
    }
}
