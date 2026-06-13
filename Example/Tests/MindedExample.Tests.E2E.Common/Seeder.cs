using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using Builder;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using QM.Common.Testing;

namespace MindedExample.Tests.E2E.Common
{
    internal class Seeder
    {
        private TestingProfile _currentTestingProfile;
        private IMindedExampleContext _context;
        private Mock<IMindedExampleContext> _mockIMindedExampleContext;
        private int? _baselineTenantId;
        private readonly HashSet<int> _usedPrimaryKeys = new HashSet<int>();

        public Seeder(TestingProfile currentTestingProfile, IMindedExampleContext context, Mock<IMindedExampleContext> mockIMindedExampleContext) {
            _currentTestingProfile = currentTestingProfile;
            _context = context;
            _mockIMindedExampleContext = mockIMindedExampleContext;
        }

        public async Task<IEnumerable<T>> Seed<T>(Expression<Func<T, int>> id, int quantity = 100, Action<T, int> buildAction = default, CancellationToken cancellationToken = default) where T : class, new()
        {
            List<T> entities = null;

            if (_currentTestingProfile == TestingProfile.UnitTesting)
            {
                entities = Builder<T>.New().BuildMany(quantity, (e, i) =>
                {
                    // Execute custom action initialization if present
                    if (buildAction != default)
                        buildAction(e, i);

                    // Set the primary key
                    SetPrimaryKey(id, e);
                });

                PropertyInfo property = _context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                ParameterExpression parameter = Expression.Parameter(typeof(IMindedExampleContext));
                MemberExpression body = Expression.PropertyOrField(parameter, property.Name);
                var lambdaExpression = Expression.Lambda<Func<IMindedExampleContext, DbSet<T>>>(body, parameter);

                Mock<DbSet<T>> mockDbSet = entities.GetMockDbSet();

                mockDbSet.Setup(s => s.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                    .Callback((T added, CancellationToken ct) =>
                    {
                        // Create the setter to simulate the creation of the ID in the entity
                        SetPrimaryKey(id, added);

                        var currentEntities = mockDbSet.Object.ToList<T>();
                        currentEntities.Add(added);
                        mockDbSet = currentEntities.GetMockDbSet();
                        _mockIMindedExampleContext.SetupGet(lambdaExpression).Returns(mockDbSet.Object);
                    });

                _mockIMindedExampleContext.SetupGet(lambdaExpression).Returns(mockDbSet.Object);
            }
            else if (_currentTestingProfile == TestingProfile.E2ELive)
            {
                _context.ChangeTracker.Clear();
                entities = Builder<T>.New().BuildMany(quantity, (e, i) => {
                    // Re-point the randomly generated TenantId at the baseline tenant
                    // before buildAction runs, so explicit per-test values still win
                    ApplyBaselineTenant(e);

                    // Execute custom action initialization if present
                    if (buildAction != default)
                        buildAction(e, i);

                    // Set the primary key
                    SetPrimaryKey(id, e);
                });
                PropertyInfo property = _context.GetType().GetProperties()
                .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(_context);
                dbSet.AddRange(entities);
                _context.SaveChanges();

                _context.ChangeTracker.Clear();
            }
            else // E2E
            {
                entities = Builder<T>.New().BuildMany(quantity, (e,i) => {
                    ApplyBaselineTenant(e);
                    SetPrimaryKey(id, e);
                    if(buildAction != null)
                        buildAction(e, i);
                });
                PropertyInfo property = _context.GetType().GetProperties()
                .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(_context);
                dbSet.AddRange(entities);
                await _context.SaveChangesAsync(cancellationToken);
                _context.ChangeTracker.Clear();
            }

            return entities;
        }

        /// <summary>
        /// Entities are built with random property values, so domain-constrained properties
        /// must be re-pointed at valid values before insert:
        /// - TenantId would reference a non-existent tenant and violate the foreign key;
        ///   it is set to the baseline tenant created by BaseE2ETest during initialization.
        /// - TenantRole is a constrained value stored as varchar(20); random strings overflow
        ///   it on providers that enforce column lengths (PostgreSQL, SQL Server).
        /// Runs before the per-test buildAction so explicit test values still win.
        /// </summary>
        private void ApplyBaselineTenant<T>(T entity) where T : class
        {
            PropertyInfo tenantRoleProperty = typeof(T).GetProperty("TenantRole");
            if (tenantRoleProperty != null && tenantRoleProperty.CanWrite && tenantRoleProperty.PropertyType == typeof(string))
                tenantRoleProperty.SetValue(entity, TenantMemberRoles.Member);

            PropertyInfo tenantIdProperty = typeof(T).GetProperty("TenantId");
            if (tenantIdProperty == null || !tenantIdProperty.CanWrite)
                return;

            if (_baselineTenantId == null)
                _baselineTenantId = _context.Tenants.Select(t => (int?)t.Id).FirstOrDefault();

            if (_baselineTenantId == null)
                return;

            tenantIdProperty.SetValue(entity, _baselineTenantId.Value);
        }

        private void SetPrimaryKey<T>(Expression<Func<T, int>> id, T e) where T : class, new()
        {
            ParameterExpression parameter1 = Expression.Parameter(typeof(T));
            ParameterExpression parameter2 = Expression.Parameter(typeof(int));

            var member = (MemberExpression)id.Body;
            var propertyInfo = (PropertyInfo)member.Member;

            MemberExpression property = Expression.Property(parameter1, propertyInfo);
            BinaryExpression assignment = Expression.Assign(property, parameter2);

            var setter = Expression.Lambda<Action<T, int>>(assignment, parameter1, parameter2);

            if (_currentTestingProfile == TestingProfile.E2E)
            {
                setter.Compile()(e, default);
                return;
            }

            setter.Compile()(e, NextUniquePrimaryKey());
        }

        /// <summary>
        /// Random primary keys must be unique within a test run: Any.Int() alone collides
        /// occasionally when seeding many rows (birthday paradox), producing flaky
        /// "instance is already being tracked" failures. Values below 1000 are also
        /// rejected so they never clash with autoincrement ids assigned to baseline
        /// rows (tenant, authenticated user) created during test initialization.
        /// </summary>
        private int NextUniquePrimaryKey()
        {
            int value;
            do
            {
                value = Any.Int();
            } while (value < 1000 || !_usedPrimaryKeys.Add(value));

            return value;
        }
    }
}
