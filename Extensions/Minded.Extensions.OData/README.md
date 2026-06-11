# Minded.Extensions.OData

Core OData utilities and extensions for Minded Framework, providing foundational OData support for querying, filtering, sorting, and pagination.

## Features

- **Filter expression extraction** - `GetFilterExpression<TEntity>()` converts an OData `$filter` into a LINQ `Expression<Func<TEntity, bool>>`
- **Direct IQueryable application** - `ApplyODataQueryOptions()` applies OData query options to any `IQueryable<T>` and unwraps `$select`/`$expand` projections back into plain entities
- **ASP.NET Core OData Integration** - Built on `Microsoft.AspNetCore.OData`
- **Trait-based CQRS flow** - Designed to work with `Minded.Extensions.CQRS.OData` (populates query trait interfaces from OData options) and `Minded.Extensions.CQRS.EntityFrameworkCore` (applies the traits to EF Core queryables)

## Installation

```bash
dotnet add package Minded.Extensions.OData
dotnet add package Microsoft.AspNetCore.OData

# Recommended companions for the trait-based flow shown below
dotnet add package Minded.Extensions.CQRS.OData
dotnet add package Minded.Extensions.CQRS.EntityFrameworkCore
```

## Quick Start

### 1. Configure OData in ASP.NET Core

```csharp
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.EntitySet<Product>("Products");
        modelBuilder.EntitySet<Order>("Orders");
        modelBuilder.EntitySet<Customer>("Customers");

        services.AddControllers()
            .AddOData(options => options
                .Select()
                .Filter()
                .OrderBy()
                .Expand()
                .Count()
                .SetMaxTop(100)
                .AddRouteComponents("odata", modelBuilder.GetEdmModel()));

        services.AddMinded(Configuration, mindedBuilderConfiguration: builder =>
        {
            builder.AddQueryLoggingDecorator();
            builder.AddQueryExceptionDecorator();
        });
    }
}
```

### 2. Create a Trait-Based Query and Handler (Recommended)

The framework-intended pattern does **not** pass `ODataQueryOptions` into the query. Instead, the query implements the CQRS **trait interfaces** (`ICanFilterExpression<T>`, `ICanOrderBy`, `ICanTop`, `ICanSkip`, `ICanCount`, `ICanExpand`); the controller populates them with `ApplyODataQueryOptions()` (from `Minded.Extensions.CQRS.OData`) and the handler applies them with `ApplyQueryTo()` (from `Minded.Extensions.CQRS.EntityFrameworkCore`). This keeps queries and handlers free of any ASP.NET Core OData dependency.

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

public class GetProductsQuery : IQuery<IQueryResponse<IEnumerable<Product>>>,
    ICanFilterExpression<Product>, ICanOrderBy, ICanTop, ICanSkip, ICanCount, ICanExpand
{
    // ICanFilterExpression<Product> - populated from $filter
    public Expression<Func<Product, bool>> Filter { get; set; }

    // ICanOrderBy - populated from $orderby
    public IList<OrderDescriptor> OrderBy { get; set; }

    // ICanTop / ICanSkip - populated from $top / $skip
    public int? Top { get; set; }
    public int? Skip { get; set; }

    // ICanCount - populated from $count
    public bool Count { get; set; }
    public bool CountOnly { get; set; }
    public int CountValue { get; set; }

    // ICanExpand - populated from $expand
    public string[] Expand { get; set; }

    public Guid TraceId { get; } = Guid.NewGuid();
}
```

The handler applies the populated traits to the EF Core `IQueryable<T>` with `ApplyQueryTo()` (extension method in the `Minded.Framework.CQRS.Query` namespace, shipped by `Minded.Extensions.CQRS.EntityFrameworkCore`):

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
        // ApplyQueryTo applies OrderBy, Expand, Filter, Count, Skip and Top
        var result = await query
            .ApplyQueryTo(_context.Products.AsNoTracking())
            .ToListAsync(cancellationToken);

        return new QueryResponse<IEnumerable<Product>>(result);
    }
}
```

### 3. Create the Controller

The controller maps the incoming OData options onto the query traits with `ApplyODataQueryOptions()` and dispatches through the mediator:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Minded.Extensions.CQRS.OData;
using Minded.Extensions.WebApi;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IRestMediator _restMediator;

    public ProductsController(IRestMediator restMediator)
    {
        _restMediator = restMediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        ODataQueryOptions<Product> queryOptions,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery();

        // Populates Filter, OrderBy, Top, Skip, Count and Expand
        // from $filter, $orderby, $top, $skip, $count and $expand
        query.ApplyODataQueryOptions(queryOptions);

        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
    }
}
```

> If you do not use `Minded.Extensions.WebApi`, inject `IMediator` instead and call `var response = await _mediator.ProcessQueryAsync(query, cancellationToken); return Ok(response.Result);`.

## OData Query Examples

### Filtering ($filter)

```http
GET /odata/Products?$filter=Price gt 100
GET /odata/Products?$filter=Category eq 'Electronics'
GET /odata/Products?$filter=Price gt 100 and Price lt 500
GET /odata/Products?$filter=contains(Name, 'Phone')
GET /odata/Products?$filter=startswith(Name, 'iPhone')
GET /odata/Products?$filter=InStock eq true
```

### Sorting ($orderby)

```http
GET /odata/Products?$orderby=Price
GET /odata/Products?$orderby=Price desc
GET /odata/Products?$orderby=Category, Price desc
GET /odata/Products?$orderby=Name asc
```

### Pagination ($top, $skip)

```http
GET /odata/Products?$top=10
GET /odata/Products?$skip=20&$top=10
GET /odata/Products?$skip=0&$top=20&$orderby=Name
```

### Selecting Fields ($select)

```http
GET /odata/Products?$select=Id,Name,Price
GET /odata/Products?$select=Name,Category
```

> `$select` is **not** mapped to a query trait by `ApplyODataQueryOptions(query, options)`. It is only honoured by the low-level `IQueryable<T>.ApplyODataQueryOptions(options)` helper (see [Low-Level Utilities](#low-level-utilities-this-package)).

### Expanding Related Data ($expand)

```http
GET /odata/Orders?$expand=Customer
GET /odata/Orders?$expand=OrderItems($expand=Product)
GET /odata/Orders?$expand=Customer,OrderItems
```

### Counting ($count)

```http
GET /odata/Products/$count
GET /odata/Products?$count=true
GET /odata/Products?$filter=Price gt 100&$count=true
```

### Complex Queries

```http
GET /odata/Products?$filter=Price gt 100&$orderby=Price desc&$top=10&$select=Name,Price
GET /odata/Orders?$expand=Customer,OrderItems($expand=Product)&$filter=Status eq 'Pending'&$orderby=OrderDate desc
```

## Advanced Usage

### Low-Level Utilities (this package)

`Minded.Extensions.OData` ships the `ODataQueryOptionExtensions` class (namespace `Minded.Extensions.OData`) with two helpers that work without the trait system:

```csharp
// Converts an OData $filter into a LINQ expression that can be applied to any IQueryable<TEntity>
public static Expression<Func<TEntity, bool>> GetFilterExpression<TEntity>(this FilterQueryOption filter)

// Applies the OData query options to the queryable and materialises the results,
// unwrapping the $select/$expand wrapper objects back into plain T instances.
// T must have a public parameterless constructor.
public static IEnumerable<T> ApplyODataQueryOptions<T>(this IQueryable<T> query, ODataQueryOptions options) where T : class, new()
```

Use these when the query intentionally carries the raw `ODataQueryOptions` (note: this couples the application layer to ASP.NET Core OData — prefer the trait-based flow above where possible):

```csharp
using Minded.Extensions.OData;

public class SearchProductsQuery : IQuery<List<Product>>
{
    public ODataQueryOptions<Product> Options { get; set; }
    public string SearchTerm { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();
}

public class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, List<Product>>
{
    private readonly ApplicationDbContext _context;

    public SearchProductsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Product>> HandleAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> products = _context.Products.AsNoTracking();

        // Apply custom filtering before the OData options
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            products = products.Where(p => p.Name.Contains(query.SearchTerm));
        }

        // ApplyODataQueryOptions materialises the results (returns IEnumerable<Product>)
        // and unwraps $select/$expand projections
        List<Product> result = products.ApplyODataQueryOptions(query.Options).ToList();
        return Task.FromResult(result);
    }
}
```

When `options` is `null` the original queryable is returned unchanged.

### OData with Caching

Caching uses `[MemoryCache]` together with `IGenerateCacheKey` (both required, from `Minded.Extensions.Caching.Memory`). The cache key must uniquely encode **all** discriminating properties of the query — queries whose traits are populated from OData options (expression filters, ordering, paging) are poor caching candidates because those values are hard to encode reliably in a key. Prefer caching simple, explicitly-parameterised queries:

```csharp
using Minded.Extensions.Caching.Decorator;            // IGenerateCacheKey
using Minded.Extensions.Caching.Memory.Decorator;     // MemoryCacheAttribute

[MemoryCache(ExpirationInSeconds = 300)]
public class GetProductByIdQuery : IQuery<Product>, IGenerateCacheKey
{
    public GetProductByIdQuery(int id) => Id = id;

    public int Id { get; }
    public Guid TraceId { get; } = Guid.NewGuid();

    // Must include ALL discriminating properties
    public string GetCacheKey() => $"Product_{Id}";
}
```

## Best Practices

### 1. Set Maximum Limits

Validate the incoming options before applying them to the query:

```csharp
queryOptions.Validate(new ODataValidationSettings { MaxTop = 100 });
```

Note that the trait-based `ApplyQueryTo()` (from `Minded.Extensions.CQRS.EntityFrameworkCore`) already caps collection results at 100 records when `$top` is not provided.

### 2. Use AsNoTracking for Read-Only Queries

```csharp
public async Task<IQueryResponse<IEnumerable<Product>>> HandleAsync(
    GetProductsQuery query, CancellationToken cancellationToken = default)
{
    var result = await query
        .ApplyQueryTo(_context.Products.AsNoTracking())
        .ToListAsync(cancellationToken);

    return new QueryResponse<IEnumerable<Product>>(result);
}
```

### 3. Validate Query Options

```csharp
public async Task<IActionResult> Get(ODataQueryOptions<Product> queryOptions)
{
    try
    {
        queryOptions.Validate(new ODataValidationSettings
        {
            MaxTop = 100,
            MaxSkip = 1000,
            AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy
        });
    }
    catch (ODataException ex)
    {
        return BadRequest(ex.Message);
    }

    // Process query...
}
```

### 4. Secure Sensitive Data

Restrict which query options clients may use via validation:

```csharp
queryOptions.Validate(new ODataValidationSettings
{
    AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy
});
// Or use [IgnoreDataMember] on sensitive properties
```

## Troubleshooting

### OData Routes Not Working

Ensure OData is properly configured:

```csharp
services.AddControllers()
    .AddOData(options => options
        .AddRouteComponents("odata", modelBuilder.GetEdmModel()));
```

### Query Options Not Applied

In the trait-based flow the options are applied manually, not by `[EnableQuery]`. Check that:

1. The controller calls `query.ApplyODataQueryOptions(queryOptions)` before dispatching the query.
2. The query class implements the trait interface matching the OData option (`$filter` → `ICanFilterExpression<T>`, `$orderby` → `ICanOrderBy`, `$top` → `ICanTop`, `$skip` → `ICanSkip`, `$count` → `ICanCount`, `$expand` → `ICanExpand`). Options without a matching trait are silently ignored.
3. The handler applies the traits, e.g. with `query.ApplyQueryTo(...)`.

### Performance Issues

1. Validate limits before applying the options:
   ```csharp
   queryOptions.Validate(new ODataValidationSettings { MaxTop = 100 });
   ```

2. Use AsNoTracking:
   ```csharp
   var products = _context.Products.AsNoTracking();
   ```

3. Add database indexes for frequently filtered/sorted fields

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.OData)
- [OData Documentation](https://www.odata.org/)
- [ASP.NET Core OData](https://docs.microsoft.com/en-us/odata/webapi/getting-started)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)

