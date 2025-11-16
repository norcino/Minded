# OData Navigation Property Serialization

## Overview

This folder contains components that control how Entity Framework navigation properties are serialized in JSON responses when using OData queries.

## The Problem

By default, when Entity Framework entities are serialized to JSON, all loaded navigation properties are included in the response. This can cause several issues:

1. **Circular Reference Errors**: When entities reference each other (e.g., Category → Transactions → Category)
2. **Performance Issues**: Loading and serializing unwanted related data
3. **Security Concerns**: Exposing more data than intended
4. **Large Response Payloads**: Including unnecessary nested objects

### Example of the Problem

Without this solution, a simple request like `GET /api/category/1` might return:

```json
{
  "id": 1,
  "name": "Electronics",
  "transactions": [
    {
      "id": 1,
      "description": "Purchase",
      "category": {
        "id": 1,
        "name": "Electronics",
        "transactions": [ ... ]  // Circular reference!
      }
    }
  ]
}
```

## The Solution

This solution ensures that navigation properties are **only serialized when explicitly requested** via OData's `$expand` parameter.

### Components

1. **`ODataExpandActionFilter`** - Action filter that runs before controller actions
   - Captures the `$expand` parameter from OData requests
   - Parses the property names (e.g., "Transactions,User")
   - Stores them in `HttpContext.Items` for later use

2. **`IgnoreNavigationPropertiesResolver`** - Custom JSON contract resolver
   - Runs during JSON serialization
   - Checks which properties were requested via `$expand`
   - Only serializes navigation properties that were explicitly expanded

3. **`ODataConstants`** - Shared constants
   - Defines the key used to store expanded properties in `HttpContext.Items`
   - Prevents magic strings and typos

4. **`ODataSerializationExtensions`** - Configuration helper
   - Simplifies setup in `Startup.cs`
   - Provides a single method to configure both components

### How It Works

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. HTTP Request: GET /api/category?$expand=Transactions         │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. ODataExpandActionFilter.OnActionExecuting()                  │
│    - Extracts "Transactions" from $expand parameter             │
│    - Stores in HttpContext.Items["ODataExpandedProperties"]     │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Controller Action Executes                                   │
│    - Query handler loads data (with .Include() if ICanExpand)   │
│    - Returns result to be serialized                            │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. IgnoreNavigationPropertiesResolver.CreateProperties()        │
│    - For each navigation property, checks if it's in the        │
│      expanded properties list from HttpContext.Items            │
│    - Sets ShouldSerialize = true only for expanded properties   │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. JSON Response: Only includes "Transactions" property         │
│    {                                                             │
│      "id": 1,                                                    │
│      "name": "Electronics",                                      │
│      "transactions": [ ... ]  // Only included because expanded │
│    }                                                             │
└─────────────────────────────────────────────────────────────────┘
```

## Usage

### Setup in Startup.cs

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddOData();

    services.AddMvc(options => options.EnableEndpointRouting = false)
        .AddODataNavigationPropertySerialization();  // ← Add this line
}
```

### Custom Navigation Property Detection

If your entities are in a different namespace than `Data.Entity`, provide a custom detection function:

```csharp
services.AddMvc(options => options.EnableEndpointRouting = false)
    .AddODataNavigationPropertySerialization(type => 
        type.Namespace?.StartsWith("MyApp.Domain") == true);
```

### API Usage Examples

**Without $expand** (navigation properties excluded):
```bash
GET /api/category/1
```
```json
{
  "id": 1,
  "name": "Electronics",
  "description": "Electronic items"
}
```

**With $expand** (navigation properties included):
```bash
GET /api/category?$expand=Transactions
```
```json
{
  "id": 1,
  "name": "Electronics",
  "transactions": [
    { "id": 1, "description": "Purchase" }
  ]
}
```

**Multiple expands**:
```bash
GET /api/transaction?$expand=Category,User
```
```json
{
  "id": 1,
  "description": "Purchase",
  "category": { "id": 1, "name": "Electronics" },
  "user": { "id": 1, "name": "John" }
}
```

## Technical Details

### Virtual Properties

The resolver only affects **virtual** navigation properties (those that support lazy loading):

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public virtual ICollection<Transaction> Transactions { get; set; }  // ← Affected
}
```

### Excluded Properties

Properties marked with `[JsonProperty]` are **not** affected by this resolver:

```csharp
public class Category
{
    [JsonProperty]
    public virtual ICollection<Transaction> Transactions { get; set; }  // ← Always serialized
}
```

### Supported $expand Formats

The filter handles various $expand formats:

- Simple: `$expand=Transactions`
- Multiple: `$expand=Transactions,User`
- Nested: `$expand=Transactions($expand=Category)` (only first level is captured)
- Paths: `$expand=Transactions/Category` (only first segment is captured)

## Troubleshooting

### Navigation properties still being serialized

1. **Check that both components are registered** in `Startup.cs`
2. **Verify the namespace** in `DefaultIsNavigationProperty` matches your entities
3. **Check for `[JsonProperty]` attributes** on navigation properties (these bypass the resolver)

### $expand not working

1. **Ensure your query implements `ICanExpand`** trait
2. **Verify the query handler calls `.Include()`** for expanded properties
3. **Check that `ApplyODataQueryOptions` is called** in the controller

### Build errors after moving files

Update the `using` statements in `Startup.cs`:

```csharp
using Application.Api.OData;  // ← Add this
```

## See Also

- [Main README - OData Support](../../../README.md#odata-support)
- [Minded.Extensions.OData Documentation](../../../Extensions/Minded.Extensions.CQRS.OData/)

