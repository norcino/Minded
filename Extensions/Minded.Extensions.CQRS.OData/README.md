# Minded.Extensions.CQRS.OData

OData integration for Minded Framework CQRS queries: maps incoming `ODataQueryOptions` onto the CQRS query **trait interfaces** so queries and handlers stay free of any ASP.NET Core OData dependency.

## Features

- **`ApplyODataQueryOptions()`** - Populates query trait interfaces from `ODataQueryOptions` in one call
- Advanced filtering with `$filter` (extracted as a LINQ `Expression<Func<T, bool>>`)
- Sorting with `$orderby`
- Pagination with `$top` and `$skip`
- Entity expansion with `$expand`
- Counting with `$count`
- Integration with ASP.NET Core OData (`Microsoft.AspNetCore.OData`)

## Installation

```bash
dotnet add package Minded.Extensions.CQRS.OData
```

## Usage

### The ApplyODataQueryOptions overloads

The `ODataQueryOptionExtensions` class (namespace `Minded.Extensions.CQRS.OData`) exposes two overloads, selected by the result shape of the query:

```csharp
// For queries returning a plain result, e.g. IQuery<Product> or IQuery<IEnumerable<Product>>
public static void ApplyODataQueryOptions<T>(this IQuery<T> query, ODataQueryOptions options)

// For queries returning a wrapped result, e.g. IQuery<IQueryResponse<IEnumerable<Product>>>
public static void ApplyODataQueryOptions<T>(this IQuery<IQueryResponse<T>> query, ODataQueryOptions options)
```

Both overloads behave identically: each OData option is copied onto the query **only when the query implements the corresponding trait interface** (from `Minded.Framework.CQRS.Query.Trait`). Options without a matching trait are silently ignored.

| OData option | Trait interface | Populated property |
|--------------|-----------------|--------------------|
| `$filter` | `ICanFilterExpression<TEntity>` | `Expression<Func<TEntity, bool>> Filter` |
| `$orderby` | `ICanOrderBy` | `IList<OrderDescriptor> OrderBy` (direction + property name, in order) |
| `$top` | `ICanTop` | `int? Top` |
| `$skip` | `ICanSkip` | `int? Skip` |
| `$count` | `ICanCount` | `bool Count` |
| `$expand` | `ICanExpand` | `string[] Expand` (comma-separated paths split; `/` replaced with `.` for nested navigation) |

> `$select` is **not** mapped to any trait. Use the low-level utilities in `Minded.Extensions.OData` if you need `$select` projection support.

### Supported result shapes for $filter extraction

The `$filter` expression is extracted for the **entity type** of the query result:

- `IQuery<Product>` — the query must implement `ICanFilterExpression<Product>`; the filter is extracted as `Expression<Func<Product, bool>>`.
- `IQuery<IEnumerable<Product>>` (any generic result with exactly **one** type argument) — the filter is extracted for the element type (`Product`) and assigned to the query's public `Filter` property via reflection; the query must implement `ICanFilterExpression<Product>`.
- `IQuery<IQueryResponse<Product>>` and `IQuery<IQueryResponse<IEnumerable<Product>>>` — handled by the second overload; the same rules apply to the type wrapped by `IQueryResponse<T>`.
- Generic results with more than one type argument (e.g. dictionaries) are **not** supported for filter extraction — the `$filter` is skipped.

If the `$filter` cannot be translated (unsupported function or invalid syntax), `ApplyODataQueryOptions` throws a `System.Exception` with the message `"Unable to extract Odata Filter, the filter might not be supported or incorrect syntax"` wrapping the original error.

### Worked example

Query implementing the traits:

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

public class GetProductsQuery : IQuery<IQueryResponse<IEnumerable<Product>>>,
    ICanFilterExpression<Product>, ICanOrderBy, ICanTop, ICanSkip, ICanCount, ICanExpand
{
    public Expression<Func<Product, bool>> Filter { get; set; }
    public IList<OrderDescriptor> OrderBy { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public bool Count { get; set; }
    public bool CountOnly { get; set; }
    public int CountValue { get; set; }
    public string[] Expand { get; set; }

    public Guid TraceId { get; } = Guid.NewGuid();
}
```

Controller populating the traits and dispatching the query:

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

        // GET /api/products?$filter=Price gt 100&$orderby=Name&$top=10&$skip=20&$count=true&$expand=Category
        // populates Filter, OrderBy, Top, Skip, Count and Expand on the query
        query.ApplyODataQueryOptions(queryOptions);

        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
    }
}
```

In the handler, apply the populated traits to an EF Core `IQueryable<T>` with `ApplyQueryTo()` from the companion package `Minded.Extensions.CQRS.EntityFrameworkCore` — see that package's README for details.

### IQueryable helper

The package also includes a low-level helper that bypasses the trait system entirely:

```csharp
public static IEnumerable<T> ApplyODataQueryOptions<T>(this IQueryable<T> query, ODataQueryOptions options) where T : class, new()
```

It applies the options directly to the queryable and materialises the result. When the options produce a plain entity sequence the result is capped at 100 records; when `$select`/`$expand` produce projection wrappers, these are unwrapped back into plain `T` instances (no cap is applied on that path). When `options` is `null` the original queryable is returned unchanged.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.CQRS.OData)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
