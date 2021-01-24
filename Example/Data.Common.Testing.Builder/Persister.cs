using System;
using System.Collections.Generic;
using System.Linq;
using Builder;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Data.Common.Testing.Builder
{
    /// <inheritdoc cref="IPersister{TE}"/>
    public class Persister<TE> : IPersister<TE>, IDisposable where TE : class, new()
    {
        private IMindedExampleContext _context;
        private readonly DbSet<TE> _dbSet;

        public static Persister<TE> New(IMindedExampleContext context)
        {
            return (Persister<TE>)Activator.CreateInstance(typeof(Persister<TE>), context);
        }
                
        public Persister(IMindedExampleContext context)
        {
            _context = context;
            _dbSet = _context.Set<TE>();
        }

        /// <inheritdoc cref="IPersister{TE}.Persist()"/>
        public virtual TE Persist()
        {
            var entity = new Builder<TE>().Build();
            entity = ResetKey(entity);
            entity = AddRequiredForeignKeyEntities(entity);
            _dbSet.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        /// <inheritdoc cref="IPersister{TE}.Persist(TE)"/>
        public virtual TE Persist(TE entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        /// <inheritdoc cref="IPersister{TE}.Persist(TE)"/>
        public virtual List<TE> Persist(int numberOfEntities, Action<TE, int> entitySetupAction = null)
        {
            if (numberOfEntities < 1)
                throw new ArgumentOutOfRangeException($"{nameof(numberOfEntities)} must be greater than zero");

            var result = new List<TE>();
            for (var i = 1; i <= numberOfEntities; i++)
            {
                var entity = new Builder<TE>().Build();

                // Execute the custom setup
                entitySetupAction?.Invoke(entity, i);

                // Reset the entity key, overriding the custom setup configuration
                entity = ResetKey(entity);              

                entity = AddRequiredForeignKeyEntities(entity);
                result.Add(entity);
            }            

            _dbSet.AddRange(result);
            _context.SaveChanges();
            return result;
        }

        /// <inheritdoc cref="IPersister{TE}.Persist(TE)"/>
        public virtual TE Persist(Action<TE> entitySetupAction)
        {
            if (entitySetupAction == null)
                throw new ArgumentNullException($"{nameof(entitySetupAction)}");

            // Generate the custom entity using the builder and the optional initialization action
            var entity = new Builder<TE>().Build();
            entitySetupAction?.Invoke(entity);

            // Reset the entity key, overriding the custom setup configuration
            entity = ResetKey(entity);

            entity = AddRequiredForeignKeyEntities(entity);

            _dbSet.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        /// <summary>
        /// Add required One To Many foreign keys, this method is vulnerable to circular reference
        /// </summary>
        /// <param name="entity">Entity where the foreign keys will be added</param>
        /// <returns>Original entity with the added foreign keys</returns>
        private TE AddRequiredForeignKeyEntities(TE entity)
        {
            // Get foreign keys of the entity type
            var foreignKeys = _context.Model.GetEntityTypes()
                .FirstOrDefault(m => m.ClrType == typeof(TE))?
                .GetForeignKeys().Where(fk => fk.IsRequired);

            // For each Foreign key with multiplicity one, create and save the Principal entity
            // Update the foreign key property and the Navigation property            
            foreach(var foreignKey in foreignKeys)
            {
                var principalEntityType = foreignKey.PrincipalEntityType.ClrType;

                // Get Principal type and generate persister for it
                var persisterType = typeof(Persister<>).MakeGenericType(principalEntityType);
                var persister = Activator.CreateInstance(persisterType);

                // Get and invoke the Persist method
                var buildMethod = persisterType.GetMethods().Single(x =>
                    x.Name == "Persist" && !x.IsGenericMethod && !x.GetParameters().Any());
                var persistedEntity = buildMethod.Invoke(persister, null);

                // Assign navigation property the created principal type                
#pragma warning disable EF1001 // Internal EF Core API usage.
                var navigationsOfTheFk = foreignKey.GetNavigations()
#pragma warning restore EF1001 // Internal EF Core API usage.
                    .FirstOrDefault(n => n.ClrType == principalEntityType);

                navigationsOfTheFk?.PropertyInfo?.SetValue(entity, persistedEntity);

                // Fore each property of the principal PrimaryKey
                foreach (var primaryKeyProperty in foreignKey.PrincipalKey.Properties)
                {
                    // Save the value to the corresponding ForeignKey property
                    var primaryKeyPropertyValue = primaryKeyProperty.PropertyInfo.GetValue(persistedEntity);
                    var foreingKeyPropertyInfo = foreignKey?.Properties.FirstOrDefault(property => 
                        property.FindFirstPrincipal() == primaryKeyProperty);
                    foreingKeyPropertyInfo?.PropertyInfo?.SetValue(entity, primaryKeyPropertyValue);
                }
            }

            return entity;          
        }

        /// <summary>
        /// Detects the primary keys mapped for the entity. If those are generated by the database, the value is automatically reset.
        /// </summary>
        /// <param name="entity">Entity where the key values are reset</param>
        /// <returns>Entity with reset keys values</returns>
        private T ResetKey<T>(T entity)
        {
            var keys = _context.Model.FindEntityType(entity.GetType())?.FindPrimaryKey();
            var properties = keys?.Properties;

            if (properties == null)
                return entity;

            foreach (var property in properties)
            {
#pragma warning disable EF1001 // Internal EF Core API usage.
                if (!property.RequiresValueGenerator())
#pragma warning restore EF1001 // Internal EF Core API usage.
                    continue;

                var keyName = property.Name;
                var entityProperty = entity.GetType().GetProperty(keyName);
                var type = entityProperty.PropertyType;

                entityProperty.SetValue(entity, type.IsValueType ? Activator.CreateInstance(type) : null);
            }
            return entity;
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                _context?.Dispose();
            }

            isDisposed = true;
        }
        #endregion
    }
}