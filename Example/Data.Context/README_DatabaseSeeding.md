# Database Seeding for Development and Debugging

## Overview

The Minded Example application includes automatic database seeding functionality to populate the database with sample data when running in development mode. This makes it easier to test and debug the application without manually creating test data.

## How It Works

### Automatic Seeding

The `DatabaseSeeder` class automatically seeds the database with sample data when:

1. **Environment**: The application is running in **Development** mode
2. **Database Type**: Using SQLiteInMemory, LocalDb, or SQL Server (non-production)
3. **Empty Database**: The database is empty (no existing data)

### Seeding is Idempotent

The seeder checks if data already exists before seeding. If any Categories, Users, or Transactions exist in the database, the seeding process is skipped. This prevents duplicate data and allows the seeder to run safely on every application startup.

## Sample Data

The seeder creates the following sample data:

### Users (3 users)
- **John Doe** (john.doe@example.com)
- **Jane Smith** (jane.smith@example.com)
- **Bob Johnson** (bob.johnson@example.com)

### Categories (10 categories)
1. **Groceries** - Food and household supplies
2. **Utilities** - Electric, water, gas, internet bills
3. **Transportation** - Gas, public transport, car maintenance
4. **Entertainment** - Movies, games, hobbies, dining out
5. **Healthcare** - Medical expenses, pharmacy, insurance
6. **Salary** - Monthly salary and bonuses
7. **Investments** - Stock dividends, interest income
8. **Shopping** - Clothing, electronics, general shopping
9. **Housing** - Rent or mortgage payments
10. **Archived Category** - Inactive category for testing

### Transactions (15 transactions)
- Mix of income (credit) and expense (debit) transactions
- Distributed across all 3 users
- Covers various categories (salary, rent, groceries, utilities, etc.)
- Spread over the last 30 days for realistic testing

## Configuration

### Using In-Memory Database (Recommended for Development)

The `appsettings.Development.json` is configured to use SQLite in-memory database by default:

```json
{
    "DatabaseType": "SQLiteInMemory"
}
```

This configuration:
- Creates a fresh database on each application start
- Automatically seeds with sample data
- Requires no external database server
- Perfect for quick debugging and testing

### Using SQL Server or LocalDb

To use SQL Server or LocalDb instead, update `appsettings.Development.json`:

```json
{
    "DatabaseType": "SQLServer",
    "ConnectionStrings": {
        "MindedExample": "server=.;database=HouseKeeping_Dev;Integrated Security=SSPI;Trust Server Certificate=true"
    }
}
```

Or for LocalDb:

```json
{
    "DatabaseType": "LocalDb",
    "ConnectionStrings": {
        "MindedExample": "Server=(localdb)\\mssqllocaldb;Database=MindedExample;Trusted_Connection=True;"
    }
}
```

## Customizing Sample Data

To customize the sample data, edit the `DatabaseSeeder.cs` file:

### Adding More Users

```csharp
private void SeedUsers()
{
    var users = new[]
    {
        new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
        // Add more users here
    };
    
    _context.Users.AddRange(users);
    _context.SaveChanges();
}
```

### Adding More Categories

```csharp
private void SeedCategories()
{
    var categories = new[]
    {
        new Category { Id = 1, Name = "Groceries", Description = "Food and household supplies", Active = true },
        // Add more categories here
    };
    
    _context.Categories.AddRange(categories);
    _context.SaveChanges();
}
```

### Adding More Transactions

```csharp
private void SeedTransactions()
{
    var baseDate = DateTime.Now.AddDays(-30);
    
    var transactions = new[]
    {
        new Transaction 
        { 
            Id = 1, 
            UserId = 1, 
            CategoryId = 6, 
            Credit = 5000.00m, 
            Debit = 0, 
            Description = "Monthly salary", 
            Recorded = baseDate.AddDays(1) 
        },
        // Add more transactions here
    };
    
    _context.Transactions.AddRange(transactions);
    _context.SaveChanges();
}
```

## Disabling Automatic Seeding

### For Development Environment

To disable seeding in development, comment out the seeding call in `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app)
{            
    if (HostingEnvironment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        
        // Comment out this line to disable seeding
        // SeedDatabaseForDevelopment(app);
    }
    // ...
}
```

### For Production Environment

Seeding is **automatically disabled** in production environments. The seeding logic only runs when:
- `IsDevelopment()` returns true, OR
- Using SQLiteInMemory database type (which is blocked in production)

## Testing with Seeded Data

Once the application starts with seeding enabled, you can immediately test the API endpoints:

### Get All Categories
```
GET /api/category
```

### Get All Transactions
```
GET /api/transaction
```

### Get Transactions for a Specific User
```
GET /api/transaction?$filter=UserId eq 1
```

### Get Transactions by Category
```
GET /api/transaction?$filter=CategoryId eq 1
```

## Benefits

1. **Faster Development** - No need to manually create test data
2. **Consistent Testing** - Same sample data across all development environments
3. **Realistic Data** - Sample data represents real-world scenarios
4. **Easy Debugging** - Known data makes it easier to trace issues
5. **No External Dependencies** - Works with in-memory database, no SQL Server required

## Notes

- The seeder uses explicit IDs for entities to ensure consistency across runs
- Transactions are dated relative to the current date (last 30 days)
- The seeder is safe to run multiple times - it won't create duplicate data
- In-memory database data is lost when the application stops
- For persistent testing data, use LocalDb or SQL Server with development configuration

