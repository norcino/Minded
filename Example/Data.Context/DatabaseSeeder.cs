using System;
using System.Linq;
using Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace Data.Context
{
    /// <summary>
    /// Provides database seeding functionality for development and testing environments.
    /// Seeds the database with sample Categories, Users, and Transactions for debugging purposes.
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly IMindedExampleContext _context;

        /// <summary>
        /// Initializes a new instance of the DatabaseSeeder class.
        /// </summary>
        /// <param name="context">The database context to seed</param>
        public DatabaseSeeder(IMindedExampleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Seeds the database with sample data if it's empty.
        /// This method is idempotent - it will only seed if no data exists.
        /// </summary>
        public void Seed()
        {
            // Check if database already has data
            if (_context.Categories.Any() || _context.Users.Any() || _context.Transactions.Any())
            {
                return; // Database already seeded
            }

            SeedUsers();
            SeedCategories();
            SeedTransactions();
        }

        /// <summary>
        /// Seeds sample users into the database.
        /// Creates a set of test users for debugging purposes.
        /// </summary>
        private void SeedUsers()
        {
            User[] users = new[]
            {
                new User
                {
                    Id = 1,
                    Name = "John",
                    Surname = "Doe",
                    Email = "john.doe@example.com"
                },
                new User
                {
                    Id = 2,
                    Name = "Jane",
                    Surname = "Smith",
                    Email = "jane.smith@example.com"
                },
                new User
                {
                    Id = 3,
                    Name = "Bob",
                    Surname = "Johnson",
                    Email = "bob.johnson@example.com"
                }
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        /// <summary>
        /// Seeds sample categories into the database.
        /// Creates common household and personal finance categories.
        /// </summary>
        private void SeedCategories()
        {
            Category[] categories = new[]
            {
                new Category
                {
                    Id = 1,
                    Name = "Groceries",
                    Description = "Food and household supplies",
                    Active = true
                },
                new Category
                {
                    Id = 2,
                    Name = "Utilities",
                    Description = "Electric, water, gas, internet bills",
                    Active = true
                },
                new Category
                {
                    Id = 3,
                    Name = "Transportation",
                    Description = "Gas, public transport, car maintenance",
                    Active = true
                },
                new Category
                {
                    Id = 4,
                    Name = "Entertainment",
                    Description = "Movies, games, hobbies, dining out",
                    Active = true
                },
                new Category
                {
                    Id = 5,
                    Name = "Healthcare",
                    Description = "Medical expenses, pharmacy, insurance",
                    Active = true
                },
                new Category
                {
                    Id = 6,
                    Name = "Salary",
                    Description = "Monthly salary and bonuses",
                    Active = true
                },
                new Category
                {
                    Id = 7,
                    Name = "Investments",
                    Description = "Stock dividends, interest income",
                    Active = true
                },
                new Category
                {
                    Id = 8,
                    Name = "Shopping",
                    Description = "Clothing, electronics, general shopping",
                    Active = true
                },
                new Category
                {
                    Id = 9,
                    Name = "Housing",
                    Description = "Rent or mortgage payments",
                    Active = true
                },
                new Category
                {
                    Id = 10,
                    Name = "Archived Category",
                    Description = "This category is no longer active",
                    Active = false
                }
            };

            _context.Categories.AddRange(categories);
            _context.SaveChanges();
        }

        /// <summary>
        /// Seeds sample transactions into the database.
        /// Creates a mix of income (credit) and expense (debit) transactions across different categories and users.
        /// Transactions are spread over the last 30 days for realistic testing.
        /// </summary>
        private void SeedTransactions()
        {
            DateTime baseDate = DateTime.Now.AddDays(-30);

            Transaction[] transactions = new[]
            {
                // User 1 - John Doe transactions
                new Transaction
                {
                    Id = 1,
                    UserId = 1,
                    CategoryId = 6, // Salary
                    Credit = 5000.00m,
                    Debit = 0,
                    Description = "Monthly salary",
                    Recorded = baseDate.AddDays(1)
                },
                new Transaction
                {
                    Id = 2,
                    UserId = 1,
                    CategoryId = 9, // Housing
                    Credit = 0,
                    Debit = 1500.00m,
                    Description = "Monthly rent payment",
                    Recorded = baseDate.AddDays(2)
                },
                new Transaction
                {
                    Id = 3,
                    UserId = 1,
                    CategoryId = 1, // Groceries
                    Credit = 0,
                    Debit = 125.50m,
                    Description = "Weekly grocery shopping",
                    Recorded = baseDate.AddDays(3)
                },
                new Transaction
                {
                    Id = 4,
                    UserId = 1,
                    CategoryId = 2, // Utilities
                    Credit = 0,
                    Debit = 85.00m,
                    Description = "Electric bill",
                    Recorded = baseDate.AddDays(5)
                },
                new Transaction
                {
                    Id = 5,
                    UserId = 1,
                    CategoryId = 3, // Transportation
                    Credit = 0,
                    Debit = 60.00m,
                    Description = "Gas station fill-up",
                    Recorded = baseDate.AddDays(7)
                },
                
                // User 2 - Jane Smith transactions
                new Transaction
                {
                    Id = 6,
                    UserId = 2,
                    CategoryId = 6, // Salary
                    Credit = 6500.00m,
                    Debit = 0,
                    Description = "Monthly salary",
                    Recorded = baseDate.AddDays(1)
                },
                new Transaction
                {
                    Id = 7,
                    UserId = 2,
                    CategoryId = 7, // Investments
                    Credit = 250.00m,
                    Debit = 0,
                    Description = "Stock dividends",
                    Recorded = baseDate.AddDays(4)
                },
                new Transaction
                {
                    Id = 8,
                    UserId = 2,
                    CategoryId = 4, // Entertainment
                    Credit = 0,
                    Debit = 75.00m,
                    Description = "Restaurant dinner",
                    Recorded = baseDate.AddDays(6)
                },
                new Transaction
                {
                    Id = 9,
                    UserId = 2,
                    CategoryId = 8, // Shopping
                    Credit = 0,
                    Debit = 299.99m,
                    Description = "New laptop accessories",
                    Recorded = baseDate.AddDays(10)
                },
                new Transaction
                {
                    Id = 10,
                    UserId = 2,
                    CategoryId = 5, // Healthcare
                    Credit = 0,
                    Debit = 45.00m,
                    Description = "Pharmacy prescription",
                    Recorded = baseDate.AddDays(12)
                },
                
                // User 3 - Bob Johnson transactions
                new Transaction
                {
                    Id = 11,
                    UserId = 3,
                    CategoryId = 6, // Salary
                    Credit = 4500.00m,
                    Debit = 0,
                    Description = "Monthly salary",
                    Recorded = baseDate.AddDays(1)
                },
                new Transaction
                {
                    Id = 12,
                    UserId = 3,
                    CategoryId = 1, // Groceries
                    Credit = 0,
                    Debit = 95.75m,
                    Description = "Supermarket shopping",
                    Recorded = baseDate.AddDays(8)
                },
                new Transaction
                {
                    Id = 13,
                    UserId = 3,
                    CategoryId = 3, // Transportation
                    Credit = 0,
                    Debit = 120.00m,
                    Description = "Monthly bus pass",
                    Recorded = baseDate.AddDays(2)
                },
                new Transaction
                {
                    Id = 14,
                    UserId = 3,
                    CategoryId = 4, // Entertainment
                    Credit = 0,
                    Debit = 45.00m,
                    Description = "Movie tickets and popcorn",
                    Recorded = baseDate.AddDays(15)
                },
                new Transaction
                {
                    Id = 15,
                    UserId = 3,
                    CategoryId = 2, // Utilities
                    Credit = 0,
                    Debit = 120.00m,
                    Description = "Internet and cable bill",
                    Recorded = baseDate.AddDays(5)
                }
            };

            _context.Transactions.AddRange(transactions);
            _context.SaveChanges();
        }
    }
}

