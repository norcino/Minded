using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;

namespace MindedExample.Infrastructure.Persistence
{
    public class MindedExampleContext : BaseContext<MindedExampleContext>, IMindedExampleContext
    {
        public MindedExampleContext(DbContextOptions options) : base(options)
        { }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Tenant> Tenants { get; set; }
        public virtual DbSet<TenantInvite> TenantInvites { get; set; }
        public virtual DbSet<TenantJoinRequest> TenantJoinRequests { get; set; }
        public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public new DbSet<T> Set<T>() where T : class, new()
        {
            return base.Set<T>();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // All DateTime values are UTC by application convention (see UtcDateTimeConverter).
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            // Get all mappings from the current assembly
            IEnumerable<Type> mappingTypes = Assembly.GetAssembly(GetType())
                .GetTypes()
                .Where(t => t.GetInterfaces()
                .Any(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

            // Get the generic Entity method of the ModelBuilder type
            MethodInfo entityMethod = typeof(ModelBuilder).GetMethods().Single(x => 
                x.Name == "ApplyConfiguration" && 
                x.IsGenericMethod &&
                x.GetParameters().FirstOrDefault()?.ParameterType.Name == "IEntityTypeConfiguration`1"
            );

            foreach (Type mappingType in mappingTypes)
            {
                // Get the type of entity to be mapped
                Type genericTypeArg = mappingType.GetInterfaces().Single().GenericTypeArguments.Single();

                // Create the method using the generic type
                MethodInfo genericEntityMethod = entityMethod.MakeGenericMethod(genericTypeArg);
                
                // Invoke the mapping method
                genericEntityMethod.Invoke(modelBuilder, [Activator.CreateInstance(mappingType)]);
            }

            // Ignore User.Roles - it's ICollection<string>, not a navigation property.
            // We populate it manually from the UserRoles join table.
            modelBuilder.Entity<User>().Ignore(u => u.Roles);

            // Configure UserRoles join table (string-based, no FK to a Roles entity)
            modelBuilder.SharedTypeEntity<Dictionary<string, object>>("UserRoles", b =>
            {
                b.Property<int>("TenantId");
                b.Property<int>("UserId");
                b.Property<string>("RoleName").HasColumnType("varchar(100)");
                b.HasKey("TenantId", "UserId", "RoleName");
                b.ToTable("UserRoles");
                b.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
                b.HasOne<Tenant>().WithMany().HasForeignKey("TenantId").OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RolePermissions join table (pure string-string, no entity references)
            modelBuilder.SharedTypeEntity<Dictionary<string, object>>("RolePermissions", b =>
            {
                b.Property<int>("TenantId");
                b.Property<string>("RoleName").HasColumnType("varchar(100)");
                b.Property<string>("PermissionName").HasColumnType("varchar(100)");
                b.HasKey("TenantId", "RoleName", "PermissionName");
                b.ToTable("RolePermissions");
                b.HasOne<Tenant>().WithMany().HasForeignKey("TenantId").OnDelete(DeleteBehavior.Cascade);
            });
        }

        public async Task<int> SaveChangesAsync()
        {
            return await base.SaveChangesAsync();
        }
    }
}
