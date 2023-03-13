using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
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

        public IEnumerable<T> Seed<T>(Expression<Func<T, int>> id, int quantity = 100, Action<T, int> buildAction = default) where T : class, new()
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
                var property = _context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                var parameter = Expression.Parameter(typeof(IMindedExampleContext));
                var body = Expression.PropertyOrField(parameter, property.Name);
                var lambdaExpression = Expression.Lambda<Func<IMindedExampleContext, DbSet<T>>>(body, parameter);

                var mockDbSet = entities.GetMockDbSet();

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
                entities = Builder<T>.New().BuildMany(quantity, (e, i) => {
                    // Execute custom action initialization if present
                    if (buildAction != default)
                        buildAction(e, i);

                    // Set the primary key
                    SetPrimaryKey(id, e);
                });
                var property = _context.GetType().GetProperties()
                .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(_context);
                dbSet.AddRange(entities);
                _context.SaveChanges();
            }
            else // E2E
            {
                entities = Builder<T>.New().BuildMany(quantity, (e,i) => {
                    SetPrimaryKey(id, e);
                    if(buildAction != null)
                        buildAction(e, i);
                });
                var property = _context.GetType().GetProperties()
                .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(_context);
                dbSet.AddRange(entities);
                _context.SaveChanges();
            }

            return entities;
        }

        private void SetPrimaryKey<T>(Expression<Func<T, int>> id, T e) where T : class, new()
        {
            var parameter1 = Expression.Parameter(typeof(T));
            var parameter2 = Expression.Parameter(typeof(int));

            var member = (MemberExpression)id.Body;
            var propertyInfo = (PropertyInfo)member.Member;

            var property = Expression.Property(parameter1, propertyInfo);
            var assignment = Expression.Assign(property, parameter2);

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
