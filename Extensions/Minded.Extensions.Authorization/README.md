# Minded.Extensions.Authorization

RBAC (role & permission) authorization decorators for Minded framework.

This extension provides attribute-driven authorization for Commands and Queries, resource-level authorization helpers, and integration points to bridge your authentication system (JWT, cookies, API keys) into Minded's authorization pipeline.

Features
--------
- Attribute-based RBAC: RequireRoles, RequirePermissions, RequireClaim, RequireAuthenticationOnly, AllowUnauthenticated.
- Resource-level checks via RequireResourceAccess which dispatches a dedicated authorization query.
- Pluggable IAuthorizationContextAccessor to map your platform's principal/claims into an AuthorizationContext.
- Pluggable IRequestAuthorizationEvaluator for custom RBAC evaluation logic.
- Eager attribute validation at startup to catch misconfigurations early.

Installation
------------

Install the NuGet package for your project:

```bash
dotnet add package Minded.Extensions.Authorization
```

Quick start (registration)
--------------------------

Register the authorization decorators when configuring Minded. Typical registration happens while building your Minded pipeline in Program.cs / Startup.cs.

```csharp
// Example: register command + query authorization decorators
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // Ensure context decorators are registered if you use RequireResourceAccess
    builder.AddCommandContextDecorator();
    builder.AddQueryContextDecorator();

    // Register authorization decorators (commands and queries)
    builder.AddCommandAuthorizationDecorator(options =>
    {
        // Optional: require authentication for all commands by default
        options.RequireAuthenticationForAllCommands = true;
    });

    builder.AddQueryAuthorizationDecorator();

    // Register handlers after registering decorators
    builder.AddCommandHandlers()
           .AddQueryHandlers();
});

// Register platform bridge implementations
services.AddAuthorizationContextAccessor<HttpContextAuthorizationContextAccessor>();
services.AddRequestAuthorizationEvaluator<DefaultRequestAuthorizationEvaluator>();
```

Notes
-----
- Decorators are applied in the order they are registered (innermost first). The execution order is therefore the reverse: last-registered decorator executes first. Register authorization decorators in the pipeline at the appropriate place (usually after context decorators and before exception/logging decorators where you want checks to happen).
- AddCommandAuthorizationDecorator and AddQueryAuthorizationDecorator will eagerly validate RBAC attributes found on discovered request types during startup and throw on invalid attribute configurations.
- AddCommandAuthorizationDecorator and AddQueryAuthorizationDecorator also auto-register a singleton IMindedContextAccessor (via TryAddSingleton) if one is not already registered, alongside the default IRequestAuthorizationEvaluator.

Attributes and examples
-----------------------

Require a permission or set of permissions:

```csharp
[RequirePermissions("Transactions.Create", "Transactions.Edit", Match = AuthorizationMatch.Any)]
public class CreateTransactionCommand : ICommand<Transaction>
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
}
```

`Match` defaults to `AuthorizationMatch.All`, meaning the caller must hold every listed permission. Set `Match = AuthorizationMatch.Any` when holding any one of the listed permissions is sufficient.

Require a role:

```csharp
[RequireRoles("Administrator")]
public class DeleteCategoryCommand : ICommand
{
    public int Id { get; set; }
}
```

Require a claim value (exact or match by property):

```csharp
[RequireClaim("tenant", Values = new[] { "tenant-a", "tenant-b" })]
public class GetTenantReportQuery : IQuery<Report>
{
    public string TenantId { get; set; }
}
```

Allow unauthenticated access to a request (opt-out of global enforce-auth):

```csharp
[AllowUnauthenticated]
public class GetPublicCatalogQuery : IQuery<IEnumerable<Item>> { }
```

Resource-level authorization example
-----------------------------------

Use RequireResourceAccess to run a custom authorization query that receives the resource id (from the request) and a caller identifier (from a claim) and returns whether the caller is permitted to operate on the specific resource.

```csharp
[RequireResourceAccess("TransactionId", "sub", typeof(CanAccessTransactionQuery))]
public class UpdateTransactionCommand : ICommand<Transaction>
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
}

// Authorization query dispatched by the decorator
public class CanAccessTransactionQuery : IQuery<bool>
{
    public Guid TransactionId { get; }
    public string CallerId { get; }

    public CanAccessTransactionQuery(Guid transactionId, string callerId)
    {
        TransactionId = transactionId;
        CallerId = callerId;
    }
}

// Handler returns either a bool or an IQueryResponse<bool>
public class CanAccessTransactionQueryHandler : IQueryHandler<CanAccessTransactionQuery, bool>
{
    public async Task<IQueryResponse<bool>> HandleAsync(CanAccessTransactionQuery query, CancellationToken cancellationToken = default)
    {
        // Implementation: check owner, collaborators, etc.
    }
}
```

Bridging your authentication system
-----------------------------------

Implement IAuthorizationContextAccessor to extract roles, permissions and claims from your platform (for example from HttpContext.User).

```csharp
public class HttpContextAuthorizationContextAccessor : IAuthorizationContextAccessor
{
    private readonly IHttpContextAccessor _http;

    public HttpContextAuthorizationContextAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public AuthorizationContext Current
    {
        get
        {
            var user = _http.HttpContext?.User;
            var hasPrincipal = user?.Identity?.IsAuthenticated == true;

            var roles = user?.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToArray() ?? Array.Empty<string>();

            var permissions = user?.Claims
                        .Where(c => string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .ToArray() ?? Array.Empty<string>();

            var claims = user?.Claims.ToDictionary(c => c.Type, c => c.Value, StringComparer.OrdinalIgnoreCase)
                         ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return new AuthorizationContext(hasPrincipal, roles, permissions, claims);
        }
    }
}

// Register it
services.AddAuthorizationContextAccessor<HttpContextAuthorizationContextAccessor>();
```

Customizing evaluation
----------------------

Replace the default evaluator with your own implementation of IRequestAuthorizationEvaluator to plug in richer rules (groups, hierarchical roles, external RBAC services, etc.):

```csharp
public class MyEvaluator : IRequestAuthorizationEvaluator
{
    public AuthorizationDecision Evaluate(Type requestType, AuthorizationDescriptor descriptor, AuthorizationContext context)
    {
        // Implement evaluation logic
    }
}

services.AddRequestAuthorizationEvaluator<MyEvaluator>();
```

AuthorizationOptions
--------------------

AddCommandAuthorizationDecorator and AddQueryAuthorizationDecorator accept an optional configuration action where you can set AuthorizationOptions (for example RequireAuthenticationForAllCommands). Example:

```csharp
builder.AddCommandAuthorizationDecorator(opts =>
{
    opts.RequireAuthenticationForAllCommands = true;
    opts.RequireAuthenticationForAllQueries = false;
});
```

Troubleshooting
---------------
- If you use RequireResourceAccess make sure to register the Minded context decorators (AddCommandContextDecorator / AddQueryContextDecorator) so that the authorization decorator can install a recursion guard when it dispatches the authorization query.
- Misconfigured attributes or missing required services will be detected at startup by the eager validator; fix the reported errors before running.

More details
------------
See the full API and examples in the project: Extensions/Minded.Extensions.Authorization
