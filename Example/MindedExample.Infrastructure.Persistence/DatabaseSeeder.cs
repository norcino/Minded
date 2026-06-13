using System;
using System.Collections.Generic;
using System.Linq;
using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;

namespace MindedExample.Infrastructure.Persistence
{
    public class DatabaseSeeder
    {
        private const string SeedAdminPasswordHash = "AQAAAAEAACcQAAAAEGyo49oAyC6bLo+02TU8A3uG4JCSH6VrCjaZgSjTRCkqe8RFae9Mg9avYT6ADcGEvA==";

        private readonly IMindedExampleContext _context;

        public DatabaseSeeder(IMindedExampleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Seed()
        {
            if (_context.Tenants.Any())
            {
                EnsureRequiredDemoActors();
                return;
            }

            SeedFromScratch();
        }

        private void SeedFromScratch()
        {
            var tenantOne = new Tenant { Name = "Default Tenant" };
            var tenantTwo = new Tenant { Name = "Contoso Demo Tenant" };
            _context.Tenants.AddRange(tenantOne, tenantTwo);
            _context.SaveChanges();

            var globalAdmin = new User
            {
                Name = "Admin",
                Surname = "Administrator",
                Email = "admin@example.com",
                TenantId = null,
                TenantRole = TenantMemberRoles.Member,
                PasswordHash = SeedAdminPasswordHash,
                IsActive = true,
                IsGlobalAdmin = true
            };

            var tenantOneAdmin = new User
            {
                Name = "Tenant",
                Surname = "Admin One",
                Email = "admin-tenant1@example.com",
                TenantId = tenantOne.Id,
                TenantRole = TenantMemberRoles.Owner,
                PasswordHash = SeedAdminPasswordHash,
                IsActive = true,
                IsGlobalAdmin = false
            };

            var john = new User { Name = "John", Surname = "Doe", Email = "john.doe@example.com", TenantId = tenantOne.Id, TenantRole = TenantMemberRoles.Member, IsActive = true };
            var jane = new User { Name = "Jane", Surname = "Smith", Email = "jane.smith@example.com", TenantId = tenantOne.Id, TenantRole = TenantMemberRoles.Member, IsActive = true };
            var bob = new User { Name = "Bob", Surname = "Johnson", Email = "bob.johnson@example.com", TenantId = tenantOne.Id, TenantRole = TenantMemberRoles.Member, IsActive = true };

            var tenantTwoAdmin = new User
            {
                Name = "Tenant",
                Surname = "Admin Two",
                Email = "admin-tenant2@example.com",
                TenantId = tenantTwo.Id,
                TenantRole = TenantMemberRoles.Owner,
                PasswordHash = SeedAdminPasswordHash,
                IsActive = true,
                IsGlobalAdmin = false
            };

            var alice = new User { Name = "Alice", Surname = "Brown", Email = "alice.brown@example.com", TenantId = tenantTwo.Id, TenantRole = TenantMemberRoles.Member, IsActive = true };
            var mark = new User { Name = "Mark", Surname = "Wilson", Email = "mark.wilson@example.com", TenantId = tenantTwo.Id, TenantRole = TenantMemberRoles.Member, IsActive = true };

            _context.Users.AddRange(globalAdmin, tenantOneAdmin, john, jane, bob, tenantTwoAdmin, alice, mark);
            _context.SaveChanges();

            tenantOne.LegalOwnerUserId = tenantOneAdmin.Id;
            tenantTwo.LegalOwnerUserId = tenantTwoAdmin.Id;
            _context.SaveChanges();

            SeedDefaultRolePermissions(tenantOne.Id);
            SeedDefaultRolePermissions(tenantTwo.Id);
            SeedUserRoles(tenantOne.Id, tenantOneAdmin.Id, john.Id, jane.Id, bob.Id);
            SeedUserRoles(tenantTwo.Id, tenantTwoAdmin.Id, alice.Id, mark.Id);

            SeedCategoriesAndTransactionsForTenantOne(john.Id, jane.Id, bob.Id);
            SeedCategoriesAndTransactionsForTenantTwo(alice.Id, mark.Id);
        }

        private void EnsureRequiredDemoActors()
        {
            var tenantOne = _context.Tenants.FirstOrDefault(t => t.Name == "Default Tenant");
            if (tenantOne == null)
            {
                tenantOne = new Tenant { Name = "Default Tenant" };
                _context.Tenants.Add(tenantOne);
                _context.SaveChanges();
            }

            var globalAdmin = _context.Users.FirstOrDefault(u => u.Email == "admin@example.com");
            if (globalAdmin == null)
            {
                globalAdmin = new User
                {
                    Name = "Admin",
                    Surname = "Administrator",
                    Email = "admin@example.com",
                    TenantId = null,
                    TenantRole = TenantMemberRoles.Member,
                    PasswordHash = SeedAdminPasswordHash,
                    IsActive = true,
                    IsGlobalAdmin = true
                };
                _context.Users.Add(globalAdmin);
            }
            else
            {
                globalAdmin.TenantId = null;
                globalAdmin.IsGlobalAdmin = true;
                globalAdmin.IsActive = true;
                globalAdmin.PasswordHash ??= SeedAdminPasswordHash;
            }

            var tenantOneAdmin = _context.Users.FirstOrDefault(u => u.Email == "admin-tenant1@example.com");
            if (tenantOneAdmin == null)
            {
                tenantOneAdmin = new User
                {
                    Name = "Tenant",
                    Surname = "Admin One",
                    Email = "admin-tenant1@example.com",
                    TenantId = tenantOne.Id,
                    TenantRole = TenantMemberRoles.Owner,
                    PasswordHash = SeedAdminPasswordHash,
                    IsActive = true,
                    IsGlobalAdmin = false
                };
                _context.Users.Add(tenantOneAdmin);
            }

            _context.SaveChanges();

            tenantOne.LegalOwnerUserId = tenantOneAdmin.Id;

            var tenantTwo = _context.Tenants.FirstOrDefault(t => t.Name == "Contoso Demo Tenant");
            if (tenantTwo == null)
            {
                tenantTwo = new Tenant { Name = "Contoso Demo Tenant" };
                _context.Tenants.Add(tenantTwo);
                _context.SaveChanges();
            }

            var tenantTwoAdmin = _context.Users.FirstOrDefault(u => u.Email == "admin-tenant2@example.com");
            if (tenantTwoAdmin == null)
            {
                tenantTwoAdmin = new User
                {
                    Name = "Tenant",
                    Surname = "Admin Two",
                    Email = "admin-tenant2@example.com",
                    TenantId = tenantTwo.Id,
                    TenantRole = TenantMemberRoles.Owner,
                    PasswordHash = SeedAdminPasswordHash,
                    IsActive = true,
                    IsGlobalAdmin = false
                };
                _context.Users.Add(tenantTwoAdmin);
                _context.SaveChanges();
            }

            tenantTwo.LegalOwnerUserId = tenantTwoAdmin.Id;
            _context.SaveChanges();

            SeedDefaultRolePermissions(tenantOne.Id);
            SeedDefaultRolePermissions(tenantTwo.Id);

            EnsureUserRole(tenantOne.Id, tenantOneAdmin.Id, Roles.TenantAdmin);
            EnsureUserRole(tenantTwo.Id, tenantTwoAdmin.Id, Roles.TenantAdmin);
        }

        private void SeedDefaultRolePermissions(int tenantId)
        {
            if (_context is not MindedExampleContext concreteContext)
            {
                return;
            }

            var existing = concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                .Where(rp => (int)rp["TenantId"] == tenantId)
                .ToList();

            // Inserted through the shared-type entity set (not raw SQL) so EF generates
            // correctly quoted, schema-qualified SQL for every database provider.
            var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
            foreach (var kvp in DefaultRolesDefinition.RolePermissions)
            {
                foreach (var permission in kvp.Value)
                {
                    var alreadyExists = existing.Any(rp =>
                        (string)rp["RoleName"] == kvp.Key &&
                        (string)rp["PermissionName"] == permission);

                    if (alreadyExists)
                    {
                        continue;
                    }

                    rolePermissions.Add(new Dictionary<string, object>
                    {
                        ["TenantId"] = tenantId,
                        ["RoleName"] = kvp.Key,
                        ["PermissionName"] = permission
                    });
                }
            }

            concreteContext.SaveChanges();
            concreteContext.ChangeTracker.Clear();
        }

        private void SeedUserRoles(int tenantId, int tenantAdminId, params int[] userIds)
        {
            EnsureUserRole(tenantId, tenantAdminId, Roles.TenantAdmin);
            foreach (var userId in userIds)
            {
                EnsureUserRole(tenantId, userId, Roles.User);
            }
        }

        private void EnsureUserRole(int tenantId, int userId, string roleName)
        {
            if (_context is not MindedExampleContext concreteContext)
            {
                return;
            }

            var exists = concreteContext.Set<Dictionary<string, object>>("UserRoles")
                .Any(ur =>
                    (int)ur["TenantId"] == tenantId &&
                    (int)ur["UserId"] == userId &&
                    (string)ur["RoleName"] == roleName);

            if (exists)
            {
                return;
            }

            concreteContext.Set<Dictionary<string, object>>("UserRoles").Add(new Dictionary<string, object>
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId,
                ["RoleName"] = roleName
            });
            concreteContext.SaveChanges();
            concreteContext.ChangeTracker.Clear();
        }

        private void SeedCategoriesAndTransactionsForTenantOne(int johnId, int janeId, int bobId)
        {
            if (_context.Categories.Any(c => c.UserId == johnId))
            {
                return;
            }

            var categories = new List<Category>
            {
                new Category { Name = "Groceries", Description = "Food and household supplies", Active = true, UserId = johnId },
                new Category { Name = "Utilities", Description = "Electric, water, gas, internet bills", Active = true, UserId = johnId },
                new Category { Name = "Transportation", Description = "Gas, public transport, car maintenance", Active = true, UserId = johnId },
                new Category { Name = "Entertainment", Description = "Movies, games, hobbies, dining out", Active = true, UserId = johnId },
                new Category { Name = "Healthcare", Description = "Medical expenses, pharmacy, insurance", Active = true, UserId = johnId },
                new Category { Name = "Salary", Description = "Monthly salary and bonuses", Active = true, UserId = johnId },
                new Category { Name = "Investments", Description = "Stock dividends, interest income", Active = true, UserId = johnId },
                new Category { Name = "Shopping", Description = "Clothing, electronics, general shopping", Active = true, UserId = johnId },
                new Category { Name = "Housing", Description = "Rent or mortgage payments", Active = true, UserId = johnId }
            };

            _context.Categories.AddRange(categories);
            _context.SaveChanges();

            var baseDate = DateTime.UtcNow.AddDays(-30);
            var transactions = new List<Transaction>
            {
                new Transaction { UserId = johnId, CategoryId = categories[5].Id, Credit = 5000.00m, Debit = 0, Description = "Monthly salary", Recorded = baseDate.AddDays(1) },
                new Transaction { UserId = johnId, CategoryId = categories[8].Id, Credit = 0, Debit = 1500.00m, Description = "Monthly rent payment", Recorded = baseDate.AddDays(2) },
                new Transaction { UserId = janeId, CategoryId = categories[5].Id, Credit = 6500.00m, Debit = 0, Description = "Monthly salary", Recorded = baseDate.AddDays(1) },
                new Transaction { UserId = janeId, CategoryId = categories[3].Id, Credit = 0, Debit = 75.00m, Description = "Restaurant dinner", Recorded = baseDate.AddDays(6) },
                new Transaction { UserId = bobId, CategoryId = categories[5].Id, Credit = 4500.00m, Debit = 0, Description = "Monthly salary", Recorded = baseDate.AddDays(1) },
                new Transaction { UserId = bobId, CategoryId = categories[0].Id, Credit = 0, Debit = 95.75m, Description = "Supermarket shopping", Recorded = baseDate.AddDays(8) }
            };

            _context.Transactions.AddRange(transactions);
            _context.SaveChanges();
        }

        private void SeedCategoriesAndTransactionsForTenantTwo(int aliceId, int markId)
        {
            if (_context.Categories.Any(c => c.UserId == aliceId))
            {
                return;
            }

            var categories = new List<Category>
            {
                new Category { Name = "Office Supplies", Description = "Books, stationery, and desk accessories", Active = true, UserId = aliceId },
                new Category { Name = "Travel", Description = "Business and personal travel expenses", Active = true, UserId = aliceId },
                new Category { Name = "Training", Description = "Courses and certifications", Active = true, UserId = aliceId },
                new Category { Name = "Freelance Income", Description = "Additional contract work income", Active = true, UserId = aliceId }
            };

            _context.Categories.AddRange(categories);
            _context.SaveChanges();

            var baseDate = DateTime.UtcNow.AddDays(-20);
            var transactions = new List<Transaction>
            {
                new Transaction { UserId = aliceId, CategoryId = categories[3].Id, Credit = 2100.00m, Debit = 0, Description = "UX consulting project", Recorded = baseDate.AddDays(2) },
                new Transaction { UserId = aliceId, CategoryId = categories[1].Id, Credit = 0, Debit = 420.00m, Description = "Conference travel", Recorded = baseDate.AddDays(4) },
                new Transaction { UserId = markId, CategoryId = categories[0].Id, Credit = 0, Debit = 88.00m, Description = "New keyboard and headset", Recorded = baseDate.AddDays(6) },
                new Transaction { UserId = markId, CategoryId = categories[2].Id, Credit = 0, Debit = 260.00m, Description = "Cloud certification exam", Recorded = baseDate.AddDays(8) }
            };

            _context.Transactions.AddRange(transactions);
            _context.SaveChanges();
        }
    }
}
