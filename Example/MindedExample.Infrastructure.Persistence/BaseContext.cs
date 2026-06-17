using System;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace MindedExample.Infrastructure.Persistence
{
    public class BaseContext<TContext> : DbContext where TContext : DbContext
    {
        protected BaseContext()
        { }

        protected BaseContext(DbContextOptions options) : base(options)
        {
        }

        public void UseTransaction(DbTransaction transaction)
        {
            Database.UseTransaction(transaction);
        }

        public new IModel Model => base.Model;

        //        public void SetEntityState(object entity, EntityState state)
        //        {
        //            Entry(entity).State = state;
        //        }

        //        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //        {
        //            modelBuilder.HasDefaultSchema(DbSchemaStrings.Dbo);
        //            modelBuilder.Configurations.AddFromAssembly(Assembly.GetAssembly(typeof(AccountDataMapping)));
        //            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        //        }

        private bool _disposed;

        public override void Dispose()
        {
            // Idempotent: the context can be registered in DI both as the concrete type and
            // behind IMindedExampleContext, in which case the container disposes it twice;
            // accessing Database on an already-disposed context throws ObjectDisposedException.
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Database?.CloseConnection();
            base.Dispose();
        }
    }
}