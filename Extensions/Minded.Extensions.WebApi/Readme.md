# RestMediator

## Benefits of using RestMediator over Mediator
`RestMediator` simplifies API development by replacing repetitive controller logic with a structured way to handle Commands and Queries consistently.\
Instead of manually wiring HTTP responses, the `RestMediator` leverages rules to automatically determine the right **HTTP status code** and **content response**.

Compared with traditional controllers, this offers:
- **Reduced boilerplate**: No repeated try/catch or IActionResult mapping.
- **Built-in CQRS support**: Commands and queries flow naturally through handlers.
- **Consistent responses**: Rules define how outcomes map to status codes.
- **Centralized cross‑cutting concerns**: Validation, exception handling, logging, and caching are applied via decorators, not inside controllers.
- **Testability**: Handlers remain isolated from HTTP infrastructure.

## How it works
The `RestMediator` wraps around the classic `Mediator`, the logic to apply a set of rules, to determine how the result of a command or query should be translated into an HTTP response. The main building blocks are the rules themselves, which describe the expected operation, the status code to return, and whether a result should be included in the response. These rules are organized and supplied by an IRestRulesProvider, which exposes lists of command and query rules.\
The evaluation of these rules is handled by the `IRulesProcessor`, which takes the request, the result produced by the mediator, and the applicable rules, and then returns the correct `IActionResult`.\
The `IMessageRestRule`, `ICommandRestRule`, and `IQueryRestRule` interfaces define the shape of rules and allow for conditions that control when a rule applies.\
Together, these parts create a pipeline where requests go through the mediator, results and operations are matched against rules, conditions are verified and the resulting rule is used by the processor create the desired HTTP response.

## RestMediator
To use the `RestMediator` in your API, you start by registering it in the dependency injection container together with the mediator and your handlers.
```c#
    services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), b =>
    {
        b.AddMediator();
        b.AddRestMediator();

        b.AddCommandValidationDecorator()
        // Register other decorators as needed
        .AddCommandHandlers();

        b.AddQueryValidationDecorator()
        // Register other decorators as needed
        .AddQueryHandlers();
    });
```

These calls configure the mediator pipeline, set up the default rule provider and processor, and register all command and query handlers in your solution.
Once everything is registered, you can inject the `IRestMediator` interface into your controllers or minimal API endpoints.

````c#
    [HttpGet]
    public async Task<IActionResult> Get(ODataQueryOptions<Category> queryOptions)
    {
        var query = new GetCategoriesQuery();
        query.ApplyODataQueryOptions(queryOptions);
        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
    }
        
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetCategoryByIdQuery(id));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Category category)
    {
        return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateCategoryCommand(category));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteCategoryCommand(id));
    }
````
The difference from using a traditional `Mediator` is that an `RestOperation` needs to be specified.
This indicates the type of operation being performed, and it is used to look up the appropriate rule.

## RestOperation

The `RestOperation` enumeration defines the type of **REST action** being executed so that the `RestMediator` can apply the correct rule when producing an HTTP response.
It covers both query and command operations.

| Operation                   | Description                                                                                              | Use with      |
| --------------------------- | -------------------------------------------------------------------------------------------------------- | ------------- |
| **Any**                     | Default value representing no specific operation, the rule is applied to all poerations.                 | Command/Query |
| **GetMany**                 | Retrieves a collection of resources. Usually returns `200 OK` with a list, or `204 No Content` if empty. | Query         |
| **GetSingle**               | Retrieves a single resource by identifier. Returns `200 OK` if found, or `404 Not Found` if missing.     | Query         |
| **AnyGet**                  | Applies to any get rule.                                                                                 | Query         |
| **Action**                  | Executes a generic command that performs an action without returning content.                            | Command       |
| **ActionWithContent**       | Executes a command that performs an action and returns a response.                                       | Command       |
| **AnyAction**               | Applies the rule to any action.                                                                          | Command       |
| **Create**                  | Creates a new resource without returning the created entity in the response body.                        | Command       |
| **CreateWithContent**       | Creates a new resource and includes the created entity or result in the response body.                   | Command       |
| **AnyCreate**               | Applies to any creation rule.                                                                            | Command       |
| **Delete**                  | Deletes an existing resource. Typically returns `204 No Content` or `404 Not Found`.                     | Command       |
| **Patch**                   | Partially updates an existing resource without returning a body.                                         | Command       |
| **PatchWithContent**        | Partially updates an existing resource and returns the updated entity or result.                         | Command       |
| **AnyPatch**                | Applies to any patch rule.                                                                               | Command       |
| **Update**                  | Updates an existing resource without returning a response body.                                          | Command       |
| **UpdateWithContent**       | Updates an existing resource and returns the updated entity or result in the response.                   | Command       |
| **AnyUpdate**               | Applies to any update rule.                                                                              | Command       |

## ContentResponse
The `ContentResponse` enumeration defines how the `RestMediator` should handle the **HTTP response body** after a command or query has been executed and a rule has been matched.\
It allows rules to indicate whether nothing should be returned, the full message should be returned, or only the result, extracted from the `ICommandResponse` or `IQueryResponse`.\
This provides a consistent way of controlling what gets serialized into the HTTP response without hardcoding it into controllers or handlers.

Here are the available options:

* **None**
  Indicates that the response body should be empty, regardless of whether the handler returned a value. This is typically used for operations like `Delete`, `Update`, or `Action` where only the status code matters (e.g., `204 No Content`).

* **Full**
  Returns the **whole object** produced by the handler. If the handler returned a plain object or a wrapper type such as `ICommandResponse`/`IQueryResponse`, the entire object is serialized as the HTTP body. This is less common in REST scenarios but can be useful if you want full control over the response shape in the handler, for example to extract validation messages from the list of `OutcomeEntry`.

* **Result**
  Extracts the `Result` property from a response wrapper (`ICommandResponse` or `IQueryResponse`) and serializes that value as the response body. If the handler returned a plain value (not wrapped), the value itself is used. This is the most common setting for query operations and for commands that return entities or result DTOs, ensuring that clients only see the relevant domain data instead of infrastructure wrappers.

## IRulesProcessor and Default Implementation
The IRulesProcessor interface is responsible for applying the rules defined for commands and queries and converting their outcomes into the appropriate `ActionResult`.\
It provides the methods used by the `RestMediator` to evaluate the operation being executed, check the rules supplied by an `IRestRulesProvider`, and determine which HTTP status code and response content should be returned.\
In other words, it is the component that takes a request, its result, and the associated `RestOperation`, and produces the final HTTP response.\
The default implementation of `IRulesProcessor` is registered automatically when you call `AddRestMediator()`, but it can also be changed to use a custom implementation of the interface.
This implementation processes both command and query rules in a predictable order, matching the first applicable rule and returning the mapped status code and response content.\
If no matching rule is found, it falls back to sensible defaults such as `200 OK` for successful queries with results, `201 Created` for create operations, `204 No Content` for deletes and updates, or `404 Not Found` when a requested resource cannot be found.\
This default behavior covers the most common REST patterns, but you can provide your own custom implementation if your application needs more specific control.

## IRestRulesProvider and Default Rules
The `IRestRulesProvider` interface is responsible for supplying the sets of rules that define how different commands and queries should be translated into HTTP responses.
It provides two main properties: `CommandRules` and `QueryRules`, which return collections of `ICommandRestRule` and `IQueryRestRule` respectively.
These rules are used by the `IRulesProcessor` to determine the appropriate HTTP status code and response content based on the operation being performed and the result of that operation.

### Default rules
Evaluation model:
- Rules are grouped by queries and commands.
- Each rule targets a specific `RestOperation` and declares:
  - HttpStatusCode to return.
  - ContentResponse to decide whether the HTTP body is empty or contains the Result (extracted from `ICommandResponse` / `IQueryResponse` when present).
  - Optional RuleConditionProperty (for `IQueryRestRule`/`ICommandRestRule`) that gates the rule based on the value/state of a property in the request/response.

The rules processor evaluates rules in a predictable order and applies the first applicable rule.\
If no rule matches, sensible fallbacks are used (noted below).

#### Query rules dafaults
| RestOperation | Status code     | ContentResponse | Conditions / Notes                                                                                                                                                                                                     |
| ------------- | --------------- | --------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GetSingle`   | `200 OK`        | `Result`        | Applied when a single resource is found. If the query returns `IQueryResponse`, the provider sets `ContentResponse.Result` so only the `Result` is serialized.                                                         |
| `GetSingle`   | `404 Not Found` | `None`          | Applied when the single resource is **not** found (i.e., the handler returns `null` or an `IQueryResponse` with `Result == null`). This is typically implemented via a `RuleConditionProperty` that checks the result. |
| `GetMany`     | `200 OK`        | `Result`        | Collections are returned with `200 OK`. Empty collections are still `200 OK` by default (no conversion to `204`).                                                                                                      |
| `AnyGet`      | —               | —               | Provided for grouping convenience; **no direct default rule** targets `AnyGet` by itself. Matching occurs on `GetSingle` or `GetMany`.                                                                                 |
| (fallback)    | `200 OK`        | `Result`        | If a query executes successfully and no specific rule has matched, a generic “successful query” fallback returns `200 OK` with `Result` (or the value) when present.                                                   |

Defaults summary (queries):

“Found” → 200 OK with body.\
“Not found” → 404 Not Found, no body.\
Collections → 200 OK (even if empty).

If the handler returns `IQueryResponse`, only Result is written to the body when a rule says `ContentResponse.Result`, to get the whole response, including the `OutcomeEntry`, `ContentResponse.Full` must be used instead.

### Command rules dafaults
The `DefaultRestRulesProvider` supplies the baseline mapping between **REST operations** and **HTTP responses**. These defaults are designed to align with common REST semantics, while also covering asynchronous operations and error scenarios.

---

#### Create

* **Success – `201 Created`**
  The resource was created successfully. The response body contains the created object.
* **Failure – `400 Bad Request`**
  The request was invalid (e.g., validation failed). The response includes details about the failure.
* **Async – `202 Accepted`**
  The request is being processed asynchronously (fire-and-forget). The response may optionally contain a URL that the client can use to poll the status of the operation.

---

#### Update

* **Success – `200 OK`**
  The resource was updated successfully, and the updated object is returned in the response body.
* **Success – `204 No Content`**
  The resource was updated successfully, but no body is returned.
* **Failure – `404 Not Found`**
  The targeted entity identifier does not exist.
* **Failure – `400 Bad Request`**
  The request was invalid (e.g., validation failed).
* **Async – `202 Accepted`**
  The update is being processed asynchronously. A polling URL may be included in the response.

---

#### Patch / Put

* **Success – `200 OK`**
  The resource was patched successfully, and the patched object is returned in the response body.
* **Success – `204 No Content`**
  The resource was patched successfully, no body returned.
* **Failure – `404 Not Found`**
  The targeted entity identifier does not exist.
* **Failure – `400 Bad Request`**
  The request was invalid.
* **Async – `202 Accepted`**
  The patch is being processed asynchronously, and a polling URL can be provided.

---

#### Delete

* **Success – `200 OK`**
  The resource was deleted successfully.
* **Success – `200 OK`**
  Returned even if the element to delete did not exist (delete is idempotent).
* **Async – `202 Accepted`**
  The delete is being processed asynchronously, with optional polling URL.

---

#### Get (collection)

* **Success – `200 OK`**
  The response contains a list of resources matching the search criteria.
* **Success – `200 OK`**
  If no resources match, the response contains an empty array.

---

#### Get specific (single resource)

* **Success – `200 OK`**
  The resource matching the identifier is returned in the response body.
* **Failure – `404 Not Found`**
  No resource was found for the provided identifier.

---

#### Action (custom command)

* **Success – `200 OK`**
  The action executed successfully and returned a response body.
* **Success – `204 No Content`**
  The action executed successfully but returned no content.
* **Failure – `400 Bad Request`**
  The request was invalid; the response contains details about the failure.
* **Async – `202 Accepted`**
  The action is being processed asynchronously; a polling URL can be provided.

---

#### Generic results

* **`401 Unauthorized`** – The request could not be executed because the client is not authenticated.
* **`403 Forbidden`** – The client is authenticated but not authorized to perform the action.
* **`405 Method Not Allowed`** – The requested HTTP method is not supported by the endpoint.
* **`500 Internal Server Error`** – An unhandled error occurred on the server.

