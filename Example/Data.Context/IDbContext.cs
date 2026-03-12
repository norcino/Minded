using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Data.Context
{
    public interface IDbContext : IDisposable
    {
//        bool LazyLoadingEnabled { get; set; }
//        bool ProxyCreationEnabled { get; set; }
//        bool AutoDetectChangesEnabled { get; set; }
//        void SetEntityState(object entity, EntityState state);
        int SaveChanges();

        DatabaseFacade Database { get; }

        ChangeTracker ChangeTracker { get; }

        Task<int> SaveChangesAsync();

        Task<int> SaveChangesAsync(CancellationToken ct);

        IModel Model { get; }
    }
}
