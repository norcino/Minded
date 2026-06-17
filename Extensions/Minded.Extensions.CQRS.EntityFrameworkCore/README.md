# Minded.Extensions.CQRS.EntityFrameworkCore

Entity Framework Core integration for Minded Framework CQRS pattern, providing the `QueryExtensions` utility class that automatically applies query trait interfaces to `IQueryable<T>` sources.

## Features

- **QueryExtensions.ApplyQueryTo()** - Extension method that automatically applies trait interfaces to IQueryable
- **Trait Interface Support** - Implements ordering, filtering, pagination, expansion, and counting
- **DbContext Integration** - Seamless integration with Entity Framework Core
- **IQueryable Support** - Efficient database queries with LINQ
- **Async/Await Support** - Full async support with CancellationToken

## Installation

```bash
dotnet add package Minded.Extensions.CQRS.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore
```

## QueryExtensions and Trait Interfaces

The core feature of this package is the `QueryExtensions.ApplyQueryTo()` extension method. It takes an `IQueryable<T>` and automatically applies operations based on which **trait interfaces** your query class implements.

### How It Works

1. Your query class implements one or more trait interfaces from `Minded.Framework.CQRS.Query.Trait`
2. In your query handler, call `query.ApplyQueryTo(queryable)` on your EF Core `IQueryable<T>`
3. The extension method checks which interfaces your query implements and applies the corresponding operations

### Available Trait Interfaces

| Interface | Required Properties | Description |
|-----------|-------------------|-------------|
| `ICanOrderBy` | `IList<OrderDescriptor> OrderBy` | Applies ordering (ascending/descending) by property name |
| `ICanExpand` | `string[] Expand` | Applies EF Core `.Include()` for eager loading related entities |
| `ICanFilterExpression<T>` | `Expression<Func<T, bool>> Filter` | Applies a LINQ `.Where()` expression filter |
| `ICanSkip` | `int? Skip` | Skips specified number of records (pagination) |
| `ICanTop` | `int? Top` | Takes specified number of records (default: 100 if not specified) |
| `ICanCount` | `bool Count`, `bool CountOnly`, `int CountValue` | Computes the total number of records matching the filter, before pagination; `CountOnly` suppresses the data rows |

### Trait Application Order

For collection queries, `ApplyQueryTo()` applies the traits in a fixed order:

1. **OrderBy** (`ICanOrderBy`)
2. **Expand** (`ICanExpand` — EF Core `Include()`)
3. **Filter** (`ICanFilterExpression<T>` — `Where()`)
4. **Count** (`ICanCount` — `CountValue` is computed here, **after** filtering and **before** `Skip`/`Take`, so it always reflects the total number of records matching the criteria, not just the returned page)
5. **Skip** (`ICanSkip`)
6. **Top** (`ICanTop` — when `Top` is not set, a default `Take(100)` cap is applied to prevent unbounded queries)

When `CountOnly` is `true`, the returned queryable yields **no data rows** — only `query.CountValue` is populated.

### Complete Example

**1. Define a Query with Trait Interfaces:**

```csharp
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;
using System.Linq.Expressions;

public class GetProductsQuery : IQuery<IQueryResponse<IEnumerable<Product>>>,
    ICanOrderBy,           // Enable ordering
    ICanExpand,            // Enable Include() for related entities
    ICanFilterExpression<Product>,  // Enable expression-based filtering
    ICanSkip,              // Enable skip (pagination)
    ICanTop,               // Enable take/top (pagination)
    ICanCount              // Enable counting
{
    // ICanOrderBy
    public IList<OrderDescriptor> OrderBy { get; set; }

    // ICanExpand
    public string[] Expand { get; set; }

    // ICanFilterExpression<Product>
    public Expression<Func<Product, bool>> Filter { get; set; }

    // ICanSkip
    public int? Skip { get; set; }

    // ICanTop
    public int? Top { get; set; }

    // ICanCount
    public bool CountOnly { get; set; }
    public bool Count { get; set; }
    public int CountValue { get; set; }

    public Guid TraceId { get; } = Guid.NewGuid();
}
```

**2. Use ApplyQueryTo in Your Handler:**

```csharp
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;

public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, IQueryResponse<IEnumerable<Product>>>
{
    private readonly ApplicationDbContext _context;

    public GetProductsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IQueryResponse<IEnumerable<Product>>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        // ApplyQueryTo automatically applies all trait interfaces!
        var result = await query
            .ApplyQueryTo(_context.Products.AsNoTracking())
            .ToListAsync(cancellationToken);

        return new QueryResponse<IEnumerable<Product>>(result);
    }
}
```

**3. Execute the Query:**

```csharp
// Create query with filtering, ordering, and pagination
var query = new GetProductsQuery
{
    Filter = p => p.Price > 100 && p.IsActive,
    OrderBy = new List<OrderDescriptor>
    {
        new OrderDescriptor(Order.Descending, "Price"),
        new OrderDescriptor(Order.Ascending, "Name")
    },
    Expand = new[] { "Category", "Supplier" },
    Skip = 20,
    Top = 10,
    Count = true
};

var response = await mediator.ProcessQueryAsync(query);
// response.Result contains the page of products (after Skip/Top)
// query.CountValue contains the total number of records matching the Filter,
// computed before Skip/Top (if Count = true)
```

### Trait Interface Details

#### ICanOrderBy

Enables dynamic ordering by property name:

```csharp
public class OrderDescriptor
{
    public Order Order { get; }      // Ascending or Descending
    public string PropertyName { get; }
}

// Usage
query.OrderBy = new List<OrderDescriptor>
{
    new OrderDescriptor(Order.Ascending, "Name"),
    new OrderDescriptor(Order.Descending, "CreatedAt")
};
```

#### ICanExpand

Enables eager loading of related entities using EF Core's `Include()`:

```csharp
// Usage
query.Expand = new[] { "Category", "Supplier.Address" };

// Translates to:
// queryable.Include("Category").Include("Supplier.Address")
```

#### ICanFilterExpression<T>

Enables strongly-typed LINQ expression filtering:

```csharp
// Usage
query.Filter = p => p.Price > 50 && p.Category.Name == "Electronics";

// Translates to:
// queryable.Where(p => p.Price > 50 && p.Category.Name == "Electronics")
```

#### ICanSkip and ICanTop

Enable pagination:

```csharp
// Usage
query.Skip = 20;  // Skip first 20 records
query.Top = 10;   // Take 10 records

// Note: If Top is not specified, defaults to 100 to prevent unbounded queries
```

#### ICanCount

Enables counting of matching records:

```csharp
// Usage
query.Count = true;      // Compute the total count into query.CountValue
query.CountOnly = false; // Also return data (set true to only get the count, with NO data rows)

// After query execution:
int totalRecords = query.CountValue;
```

`CountValue` is computed **after** the filter is applied and **before** `Skip`/`Take`, so it always contains the total number of records matching the query criteria — not the size of the returned page. This makes it suitable for building pagination metadata (e.g. total page count) alongside a paged result.

When `CountOnly = true`, `ApplyQueryTo()` returns a queryable that yields **no data rows**; only `query.CountValue` is populated.

### OData Integration

When used with `Minded.Extensions.CQRS.OData`, you can automatically populate trait interface properties from OData query strings:

```csharp
using Minded.Extensions.CQRS.OData;

[HttpGet]
public async Task<IActionResult> GetProducts(ODataQueryOptions<Product> options)
{
    var query = new GetProductsQuery();

    // ApplyODataQueryOptions populates all trait interface properties
    // from OData query string: $filter, $orderby, $top, $skip, $count, $expand
    query.ApplyODataQueryOptions(options);

    var response = await _mediator.ProcessQueryAsync(query);
    return Ok(response.Result);
}
```

**OData URL Example:**

```http
GET /api/products?$filter=Price gt 100&$orderby=Name asc&$top=10&$skip=20&$expand=Category&$count=true
```

This automatically maps to:

- `Filter` ← `$filter=Price gt 100`
- `OrderBy` ← `$orderby=Name asc`
- `Top` ← `$top=10`
- `Skip` ← `$skip=20`
- `Expand` ← `$expand=Category`
- `Count` ← `$count=true`

## Quick Start

### 1. Create DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
}
```

### 2. Create a Simple Query (Without Traits)

For simple queries that don't need dynamic filtering/ordering:

```csharp
using Minded.Framework.CQRS.Query;

public class GetUserByIdQuery : IQuery<User>
{
    public int UserId { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, User>
{
    private readonly ApplicationDbContext _context;

    public GetUserByIdQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
    }
}
```

### 3. Configure Services

```csharp
using Minded.Extensions.Configuration;

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

services.AddMinded(Configuration, mindedBuilderConfiguration: builder =>
{
    builder.AddQueryLoggingDecorator();
    builder.AddQueryExceptionDecorator();
});
```

## Query Patterns

> **Note**: For dynamic filtering, ordering, and pagination, the query **must** implement the trait interfaces and the handler must call `ApplyQueryTo()`, as shown below and in the [QueryExtensions and Trait Interfaces](#queryextensions-and-trait-interfaces) section above. Traits that are not implemented are simply not applied.

### Query with Trait Interfaces

```csharp
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

// The query MUST implement the trait interfaces for ApplyQueryTo to work
public class GetUsersQuery : IQuery<IQueryResponse<IEnumerable<User>>>,
    ICanOrderBy,    // Required for ordering
    ICanSkip,       // Required for skip pagination
    ICanTop         // Required for take/limit pagination
{
    // Each trait interface requires specific properties
    public IList<OrderDescriptor> OrderBy { get; set; }
    public int? Skip { get; set; }
    public int? Top { get; set; }

    public Guid TraceId { get; } = Guid.NewGuid();
}

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IQueryResponse<IEnumerable<User>>>
{
    private readonly ApplicationDbContext _context;

    public GetUsersQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IQueryResponse<IEnumerable<User>>> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        // ApplyQueryTo checks which traits are implemented and applies them
        var result = await query
            .ApplyQueryTo(_context.Users.AsNoTracking())
            .ToListAsync(cancellationToken);

        return new QueryResponse<IEnumerable<User>>(result);
    }
}
```

## Single-Entity Queries

In addition to the collection overloads (which return a composable `IQueryable<T>`), `ApplyQueryTo()` provides two **async** overloads for queries returning a single entity:

```csharp
// For queries declared as IQuery<T>:
// returns the first matching entity, or null when no match is found
public static Task<T> ApplyQueryTo<T>(this IQuery<T> query, IQueryable<T> queryable) where T : class

// For queries declared as IQuery<IQueryResponse<T>>
public static Task<IQueryResponse<T>> ApplyQueryTo<T>(this IQuery<IQueryResponse<T>> query, IQueryable<T> queryable) where T : class
```

Both overloads apply only the `ICanExpand` (`Include()`) and `ICanFilterExpression<T>` (`Where()`) traits, then materialise the result with `FirstOrDefaultAsync()`. The `ICanCount`, `ICanTop`, `ICanSkip` and `ICanOrderBy` traits are **intentionally ignored** because they are not meaningful for single-entity queries.

```csharp
using System.Linq.Expressions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

public class GetProductByIdQuery : IQuery<Product>, ICanFilterExpression<Product>, ICanExpand
{
    public GetProductByIdQuery(int id)
    {
        Filter = p => p.Id == id;
    }

    public Expression<Func<Product, bool>> Filter { get; set; }
    public string[] Expand { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, Product>
{
    private readonly ApplicationDbContext _context;

    public GetProductByIdQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        // Applies Filter and Expand, then materialises with FirstOrDefaultAsync();
        // returns null when no entity matches
        return await query.ApplyQueryTo(_context.Products.AsNoTracking());
    }
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.CQRS.EntityFrameworkCore)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
