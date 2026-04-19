using System;
using System.Collections.Generic;
using System.Linq;
using MindedExample.Domain;
using Microsoft.EntityFrameworkCore;

namespace MindedExample.Infrastructure.Persistence
{
    public class DatabaseSeeder
    {
        private readonly IMindedExampleContext _context;

        public DatabaseSeeder(IMindedExampleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Seed()
        {
            if (_context.Categories.Any() || _context.Users.Any() || _context.Transactions.Any())
            {
                return;
            }

            SeedUsers();
            SeedDefaultRolePermissions();
            SeedUserRoles();
            SeedCategories();
            SeedTransactions();
        }

        private void SeedUsers()
        {
            User[] users =
            [
                new User { Id = 1, Name = "Admin", Surname = "Administrator", Email = "admin@example.com" },
                new User { Id = 2, Name = "John", Surname = "Doe", Email = "john.doe@example.com" },
                new User { Id = 3, Name = "Jane", Surname = "Smith", Email = "jane.smith@example.com" },
                new User { Id = 4, Name = "Bob", Surname = "Johnson", Email = "bob.johnson@example.com" }
            ];

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        /// <summary>
        /// Seeds the RolePermissions join table from DefaultRolesDefinition.
        /// </summary>
        private void SeedDefaultRolePermissions()
        {
            if (_context is MindedExampleContext concreteContext)
            {
                foreach (var kvp in DefaultRolesDefinition.RolePermissions)
                {
                    foreach (var permission in kvp.Value)
                    {
                        concreteContext.Database.ExecuteSqlRaw(
                            "INSERT INTO RolePermissions (RoleName, PermissionName) VALUES ({0}, {1})",
                            kvp.Key, permission);
                    }
                }
            }
        }

        /// <summary>
        /// Seeds the UserRoles join table. Admin user gets Admin role, others get User role.
        /// </summary>
        private void SeedUserRoles()
        {
            if (_context is MindedExampleContext concreteContext)
            {
                concreteContext.Database.ExecuteSqlRaw(
                    "INSERT INTO UserRoles (UserId, RoleName) VALUES ({0}, {1})", 1, Roles.Admin);
                concreteContext.Database.ExecuteSqlRaw(
                    "INSERT INTO UserRoles (UserId, RoleName) VALUES ({0}, {1})", 2, Roles.User);
                concreteContext.Database.ExecuteSqlRaw(
                    "INSERT INTO UserRoles (UserId, RoleName) VALUES ({0}, {1})", 3, Roles.User);
                concreteContext.Database.ExecuteSqlRaw(
                    "INSERT INTO UserRoles (UserId, RoleName) VALUES ({0}, {1})", 4, Roles.User);
            }
        }

        private void SeedCategories()
        {
            Category[] categories =
            [
                new Category { Id = 1, Name = "Groceries", Description = "Food and household supplies", Active = true, UserId = 2 },
                new Category { Id = 2, Name = "Utilities", Description = "Electric, water, gas, internet bills", Active = true, UserId = 2 },
                new Category { Id = 3, Name = "Transportation", Description = "Gas, public transport, car maintenance", Active = true, UserId = 2 },
                new Category { Id = 4, Name = "Entertainment", Description = "Movies, games, hobbies, dining out", Active = true, UserId = 2 },
                new Category { Id = 5, Name = "Healthcare", Description = "Medical expenses, pharmacy, insurance", Active = true, UserId = 2 },
                new Category { Id = 6, Name = "Salary", Description = "Monthly salary and bonuses", Active = true, UserId = 2 },
                new Category { Id = 7, Name = "Investments", Description = "Stock dividends, interest income", Active = true, UserId = 2 },
                new Category { Id = 8, Name = "Shopping", Description = "Clothing, electronics, general shopping", Active = true, UserId = 2 },
                new Category { Id = 9, Name = "Housing", Description = "Rent or mortgage payments", Active = true, UserId = 2 },
                new Category { Id = 10, Name = "Archived Category", Description = "This category is no longer active", Active = false, UserId = 2 }
            ];
            _context.Categories.AddRange(categories);
            _context.SaveChanges();

            Category[] subcategories =
            [
                new Category { Id = 11, Name = "Cinema", Description = "Movie tickets and cinema outings", Active = true, UserId = 2, ParentId = 4 },
                new Category { Id = 12, Name = "Internet", Description = "Internet service bills", Active = true, UserId = 2, ParentId = 2 },
                new Category { Id = 13, Name = "Gas", Description = "Gas supply bills", Active = true, UserId = 2, ParentId = 2 },
                new Category { Id = 14, Name = "Electricity", Description = "Electricity supply bills", Active = true, UserId = 2, ParentId = 2 }
            ];
            _context.Categories.AddRange(subcategories);
            _context.SaveChanges();
        }

        private void SeedTransactions()
        {
            DateTime baseDate = DateTime.Now.AddDays(-30);
            Transaction[] transactions =
            [
                new Transaction { Id = 1, UserId = 2, CategoryId = 6, Credit = 5000.00m, Debit = 0, Description = "Monthly salary", Recorded = baseDate.AddDays(1) },
                new Transaction { Id = 2, UserId = 2, CategoryId = 9, Credit = 0, Debit = 1500.00m, Description = "Monthly rent payment", Recorded = baseDate.AddDays(2) },
                new Transaction { Id = 3, UserId = 2, CategoryId = 1, Credit = 0, Debit = 125.50m, Description = "Weekly grocery shopping", Recorded = baseDate.AddDays(3) },
                new Transaction { Id = 4, UserId = 2, CategoryId = 2, Credit = 0, Debit = 85.00m, Description = "Electric bill", Recorded = baseDate.AddDays(5) },
                new Transaction { Id = 5, UserId = 2, CategoryId = 3, Credit = 0, Debit = 60.00m, Description = "Gas station fill-up", Recorded = baseDate.AddDays(7) },
                new Transaction { Id = 6, UserId = 3, CategoryId = 6, Credit = 6500.00m, Debit = 0, Description = "Monthly salary", Recorded = baseDate.AddDays(1) },
                new Transaction { Id = 7, UserId = 3, CategoryId = 7, Credit = 250.00m, Debit = 0, Description = "Stock dividends", Recorded = baseDate.AddDays(4) },
                new Transaction { Id = 8, UserId = 3, CategoryId = 4, Credit = 0, Debit = 75.00m, Description = "Restaurant dinner", Recorded = baseDate.AddDays(6) },
                new Transaction { Id = 9, UserId = 3, CategoryId = 8, Credit = 0, Debit = 299.99m, Description = "New laptop accessories", Recorded = baseDate.AddDays(10) },
                new Transaction { Id = 10, UserId = 3, CategoryId = 5, Credit = 0, Debit = 45.00m, Description = "Pharmacy prescription", Recorded = baseDate.AddDays(12) },
                new Transaction { Id = 11, UserId = 4, CategoryId = 6, Credit = 4500.00m, Debit = 0, Description = "Monthly salary", Recorded = baseDate.AddDays(1) },
                new Transaction { Id = 12, UserId = 4, CategoryId = 1, Credit = 0, Debit = 95.75m, Description = "Supermarket shopping", Recorded = baseDate.AddDays(8) },
                new Transaction { Id = 13, UserId = 4, CategoryId = 3, Credit = 0, Debit = 120.00m, Description = "Monthly bus pass", Recorded = baseDate.AddDays(2) },
                new Transaction { Id = 14, UserId = 4, CategoryId = 4, Credit = 0, Debit = 45.00m, Description = "Movie tickets and popcorn", Recorded = baseDate.AddDays(15) },
                new Transaction { Id = 15, UserId = 4, CategoryId = 2, Credit = 0, Debit = 120.00m, Description = "Internet and cable bill", Recorded = baseDate.AddDays(5) }
            ];
            _context.Transactions.AddRange(transactions);
            _context.SaveChanges();
        }
    }
}
