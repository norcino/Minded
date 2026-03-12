using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using Builder;
using Common.Tests;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Moq;
using QM.Common.Testing;

namespace Common.E2ETests
{
    internal class Seeder
    {
        private TestingProfile _currentTestingProfile;
        private IMindedExampleContext _context;
        private Mock<IMindedExampleContext> _mockIMindedExampleContext;

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

            setter.Compile()(e, Any.Int());
        }
    }
}
