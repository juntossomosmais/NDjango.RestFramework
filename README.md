# NDjango.RestFramework

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=juntossomosmais_NDjango.RestFramework&metric=coverage&token=93bbe8eaf38e963f74fc77f356cecf3583fe5600)](https://sonarcloud.io/summary/new_code?id=juntossomosmais_NDjango.RestFramework)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=juntossomosmais_NDjango.RestFramework&metric=alert_status&token=93bbe8eaf38e963f74fc77f356cecf3583fe5600)](https://sonarcloud.io/summary/new_code?id=juntossomosmais_NDjango.RestFramework)

NDjango Rest Framework makes you focus on business, not on boilerplate code. It's designed to follow the famous Django's slogan "The web framework for perfectionists with deadlines."

This is a copy of the convention established by [Django REST framework](https://github.com/encode/django-rest-framework), though translated to C# and adapted to the .NET Core framework.

## Quickstart

We'll build a CRUD API for `Person` and `TodoItem` entities step by step.

### 1. Define your entities

Entities inherit from `BaseModel<TPrimaryKey>` and implement `GetFields()` to control which fields appear in API responses.

```csharp
using NDjango.RestFramework.Base;

public abstract class StandardEntity : BaseModel<int>
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Person : StandardEntity
{
    public IList<TodoItem>? TodoItems { get; set; }
    public string Name { get; set; }

    public override string[] GetFields()
    {
        return ["Id", "Name", "CreatedAt", "UpdatedAt"];
    }
}

public class TodoItem : StandardEntity
{
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public Person Person { get; set; }
    public int UserId { get; set; }

    public override string[] GetFields()
    {
        return
        [
            "Id", "Name", "IsComplete", "CreatedAt", "UpdatedAt",
            "UserId",
            "Person",        // Include the navigation property
            "Person:Name",   // Include specific fields from Person using ":"
        ];
    }
}
```

The `:` syntax in `GetFields()` controls nested serialization. `"Person"` alone would include the entire `Person` object; `"Person:Name"` restricts it to only the `Name` field.

### 2. Define your DTOs

DTOs inherit from `BaseDto<TPrimaryKey>`. They represent the shape of data accepted in request bodies (POST, PUT, PATCH).

```csharp
using NDjango.RestFramework.Base;

public class PersonDto : BaseDto<int>
{
    public string Name { get; set; }
}

public class TodoItemDto : BaseDto<int>
{
    public string Name { get; set; }
    public bool IsComplete { get; set; }
    public int UserId { get; set; }
}
```

### 3. Set up the DbContext

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Person> Person { get; set; }
    public DbSet<TodoItem> TodoItem { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

### 4. Create the controllers

Inherit from `BaseController<TOrigin, TDestination, TPrimaryKey, TContext>` where:

| Parameter      | Meaning                  |
|----------------|--------------------------|
| `TOrigin`      | The DTO type             |
| `TDestination` | The entity type          |
| `TPrimaryKey`  | The primary key type     |
| `TContext`      | The DbContext type       |

Set `AllowedFields` to control which fields can be filtered, searched, and sorted. Add filters in the constructor:

```csharp
using Microsoft.AspNetCore.Mvc;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using NDjango.RestFramework.Serializer;

[ApiController]
[Route("api/v1/[controller]")]
public class PersonsController : BaseController<PersonDto, Person, int, AppDbContext>
{
    public PersonsController(
        Serializer<PersonDto, Person, int, AppDbContext> serializer,
        AppDbContext context,
        ILogger<Person> logger
    ) : base(serializer, context, logger)
    {
        AllowedFields =
        [
            nameof(Person.Id),
            nameof(Person.Name),
            nameof(Person.CreatedAt),
            nameof(Person.UpdatedAt),
        ];

        Filters.Add(new QueryStringFilter<Person>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Person>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Person, int>());
    }
}
```

This single controller gives you all these endpoints:

| Method   | Route                   | Description                    |
|----------|-------------------------|--------------------------------|
| `GET`    | `/api/v1/Persons`       | List with pagination & sorting |
| `GET`    | `/api/v1/Persons/{id}`  | Get single                     |
| `POST`   | `/api/v1/Persons`       | Create                         |
| `PUT`    | `/api/v1/Persons/{id}`  | Full update                    |
| `PATCH`  | `/api/v1/Persons/{id}`  | Partial update                 |
| `DELETE` | `/api/v1/Persons/{id}`  | Delete                         |
| `DELETE` | `/api/v1/Persons?ids=`  | Bulk delete                    |

There is intentionally no HTTP bulk-update verb. DRF takes the same position — `ListSerializer.update` raises `NotImplementedError` because broadcasting one body to many rows is mass-assignment, not a generic update. If you need to apply the same payload to many rows from non-HTTP code, call `Serializer.UpdateManyAsync` directly.

### 5. Include navigation properties with a custom filter

To eagerly load related entities (like `Person` inside `TodoItem`), create a filter:

```csharp
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Filters;

public class TodoItemIncludePersonFilter : Filter<TodoItem>
{
    public override IQueryable<TodoItem> AddFilter(IQueryable<TodoItem> query, HttpRequest request)
    {
        return query.Include(x => x.Person);
    }
}
```

Then add it to the controller alongside other filters:

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class TodoItemsController : BaseController<TodoItemDto, TodoItem, int, AppDbContext>
{
    public TodoItemsController(
        Serializer<TodoItemDto, TodoItem, int, AppDbContext> serializer,
        AppDbContext context,
        ILogger<TodoItem> logger
    ) : base(serializer, context, logger)
    {
        AllowedFields =
        [
            nameof(TodoItem.UserId),
            nameof(TodoItem.Name),
            nameof(TodoItem.IsComplete),
            nameof(TodoItem.CreatedAt),
            nameof(TodoItem.UpdatedAt)
        ];

        Filters.Add(new QueryStringFilter<TodoItem>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<TodoItem, int>());
        Filters.Add(new TodoItemIncludePersonFilter());
    }
}
```

### 6. Configure Program.cs

Register Newtonsoft.Json (required), then call `AddNDjangoRestFramework()` for the rest:

```csharp
using NDjango.RestFramework.Extensions;
using Newtonsoft.Json;

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("AppDbContext")));

// Controllers + Newtonsoft.Json
builder.Services.AddControllers()
    .AddNewtonsoftJson(config =>
    {
        config.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        config.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

// Wires NDjango.RestFramework in one call:
//   - scans the calling assembly for Serializer<,,,> subclasses and registers them
//   - registers the startup hosted service that asserts GetFields()/AllowedFields/Validate{X}Async are correct
//   - wires the ValidationErrors response shape onto ApiBehaviorOptions
builder.Services.AddNDjangoRestFramework();
```

`AddNewtonsoftJson` is required because the library uses Newtonsoft.Json internally for serialization and field filtering.

#### Customizing `AddNDjangoRestFramework`

`AddNDjangoRestFramework` accepts an optional `Action<NDjangoRestFrameworkOptions>` if you need to scan additional assemblies or skip the startup field-validation hosted service:

```csharp
builder.Services.AddNDjangoRestFramework(opts =>
{
    // Add other assemblies that contain serializers (calling assembly is always scanned).
    opts.Assemblies.Add(typeof(SomeSharedSerializer).Assembly);

    // Skip the startup validator (e.g., test fixtures that intentionally register bad controllers).
    // Defaults to true — leave it on in production so misconfigured fields fail at startup, not runtime.
    opts.RunStartupValidation = false;
});
```

Manual registrations still win — the scan uses `TryAddScoped`, so any closed-base mapping you wired before calling `AddNDjangoRestFramework` is preserved.

## API Guide

### Filters

Filters are applied to `GET /{id}` and `GET /` (list) requests. They are composed sequentially — each filter receives the `IQueryable` from the previous one.

#### QueryStringFilter

Matches query parameters to entity fields using exact equality. Only fields listed in `AllowedFields` are accepted.

```
GET /api/v1/Persons?Name=Iago
GET /api/v1/TodoItems?IsComplete=true&UserId=1
```

#### QueryStringSearchFilter

Searches across all `AllowedFields` with a single `search` parameter. Uses `LIKE` for string fields, exact match for other types. Results are combined with OR.

```
GET /api/v1/Persons?search=Iago
```

#### QueryStringIdRangeFilter

Filters by multiple IDs using the `ids` parameter:

```
GET /api/v1/Persons?ids=1,2,3
GET /api/v1/Persons?ids=1&ids=2&ids=3
```

#### Custom filters

`Filter<TEntity>` receives an `IQueryable` and an `HttpRequest`, so you can use it for any query modification — not just filtering. Eager loading (`.Include()`), conditional joins, or any EF Core operation:

```csharp
public class ActiveOnlyFilter : Filter<Person>
{
    public override IQueryable<Person> AddFilter(IQueryable<Person> query, HttpRequest request)
    {
        return query.Where(p => p.IsActive);
    }
}
```

### Sorting

Sorting is built-in and driven by `AllowedFields`. Use `sort` for ascending or `sortDesc` for descending. Multiple fields are comma-separated. Default: ascending by `Id`.

```
GET /api/v1/Persons?sort=Name
GET /api/v1/Persons?sortDesc=CreatedAt
GET /api/v1/Persons?sort=Name,CreatedAt
```

### Pagination

The default pagination is `PageNumberPagination`, which behaves like [DRF's PageNumberPagination](https://www.django-rest-framework.org/api-guide/pagination/#pagenumberpagination). Use `page` and `page_size` query parameters:

```
GET /api/v1/Persons?page=2&page_size=5
```

Response:

```json
{
  "count": 13,
  "next": "http://localhost:8000/api/v1/Persons?page=3&page_size=5",
  "previous": "http://localhost:8000/api/v1/Persons?page=1&page_size=5",
  "results": [
    {
      "name": "Sal Paradise",
      "createdAt": "2024-10-19T19:22:12.0524797",
      "id": 6
    }
  ]
}
```

Defaults: `page_size=5`, max `page_size=50`. You can customize by passing your own `IPagination<TDestination>` to the `BaseController` constructor.

### Partial updates (PATCH)

`PATCH` only updates the fields present in the request body. Absent fields are left unchanged. This is handled internally by `PartialJsonObject<T>`, which tracks which JSON fields were actually sent.

```
PATCH /api/v1/Persons/1
Content-Type: application/json

{"name": "New Name"}
```

Only `Name` is updated; `CreatedAt`, `UpdatedAt`, etc. remain untouched.

### Disabling endpoints

Use `ActionOptions` to disable PUT, PATCH, or the bulk-delete endpoint:

```csharp
public PersonsController(...)
    : base(serializer, context, new ActionOptions { AllowPatch = false }, logger)
{ }
```

| Flag | Default | Controls |
|---|---|---|
| `AllowPatch` | `true` | `PATCH /{id}` |
| `AllowPut` | `true` | `PUT /{id}` |
| `AllowBulkDelete` | `false` | `DELETE ?ids=` (the bulk-delete endpoint, opt-in) |

Disabled endpoints return `405 Method Not Allowed`. **Bulk delete is opt-in** — set `AllowBulkDelete = true` to expose `DELETE ?ids=`. The opt-in default is intentional: the bulk path runs a single `ExecuteDeleteAsync` SQL statement and silently bypasses `ValidateDestroyAsync`, any override of `Serializer.DestroyAsync(instance, ct)`, EF Core `SaveChanges` interceptors, audit-on-delete hooks, and soft-delete logic. Enable it only when those seams either don't exist on this resource or carry rules the bulk path is allowed to skip.

### Queryset scope on writes (DRF parity)

Every action that resolves an id to an entity composes the controller's `Filters` chain into the load step. A row-scoping filter (tenant, soft-delete, multi-account) protects writes the same way it protects reads — there is no separate path that bypasses the filter chain on `PUT`, `PATCH`, `DELETE`, or `DELETE ?ids=`.

The flow mirrors DRF mixins ([rest_framework/mixins.py:58-67 and :79-84](https://github.com/encode/django-rest-framework/blob/3.17.1/rest_framework/mixins.py#L58-L84) at tag 3.17.1): the view (controller) does `instance = self.get_object()`, then hands the loaded `instance` to the serializer. The serializer is queryset-naive — it never re-loads. Concretely:

```csharp
var query = FilterQuery(GetQuerySet(), HttpContext.Request);
var instance = await _serializer.GetObjectAsync(query, id, ct);
if (instance is null) return NotFound();
await PerformUpdateAsync(instance, origin, ct); // or PerformPartialUpdateAsync / PerformDestroyAsync
```

The contract:

| Action | Load step | Effect |
|---|---|---|
| `GET /{id}` | `GetObjectAsync(filteredQuery, id)` | Out-of-scope id → `null` → `404` |
| `PUT /{id}` | `GetObjectAsync(filteredQuery, id)` → `UpdateAsync(instance, ...)` | Out-of-scope id → `null` → `404`, no mutation |
| `PATCH /{id}` | `GetObjectAsync(filteredQuery, id)` → `PartialUpdateAsync(instance, ...)` | Out-of-scope id → `null` → `404`, no mutation |
| `DELETE /{id}` | `GetObjectAsync(filteredQuery, id)` → `DestroyAsync(instance, ct)` | Out-of-scope id → `null` → `404`, no delete |
| `DELETE ?ids=` | `DestroyManyAsync(filteredQuery, ids)` | Out-of-scope ids silently dropped from the delete set |

A request that targets a row outside the caller's scope surfaces as `404` — the same outcome as a missing row, with no information leak about whether the row exists in another tenant.

A minimal tenant filter:

```csharp
public class TenantFilter : Filter<Store>
{
    public override IQueryable<Store> AddFilter(IQueryable<Store> query, HttpRequest request)
    {
        if (!request.Headers.TryGetValue("X-Tenant", out var values) || string.IsNullOrWhiteSpace(values.ToString()))
            return query.Where(_ => false);

        var tenant = values.ToString();
        return query.Where(s => s.Tenant == tenant);
    }
}
```

Add it to the controller's `Filters` list alongside `QueryStringFilter`, `QueryStringSearchFilter`, etc. — the same way you would for a read-side filter. The defensive empty-set fallback when the header is absent is intentional: a tenant filter that silently falls back to "all rows" defeats the purpose.

For predicates that depend on the loaded row's state (not just on the request) — for example "only allow update if the row is in a state the caller can transition out of" — validate the `instance` inside a `Perform*Async` override. The hook receives the already-loaded entity, so the override is the canonical seam; there is no longer a query to compose extra `Where` clauses onto.

#### Headless callers

The serializer's public surface is queryset-naive for single-row writes. Headless callers — message consumers, scheduled jobs, admin scripts — load the instance themselves and pass it in:

```csharp
var instance = await _dbContext.Set<Person>()
    .Where(p => p.Tenant == currentTenant)
    .FirstOrDefaultAsync(p => p.Id == personId, ct);
if (instance is null) return;

await serializer.UpdateAsync(instance, dto, ct);
// or serializer.PartialUpdateAsync(instance, partial, ct);
// or serializer.DestroyAsync(instance, ct);
```

The bulk-execute methods (`UpdateManyAsync`, `DestroyManyAsync`) still take `IQueryable<TDestination>` because they project the queryset directly into the SQL `UPDATE` / `DELETE`. Pass `_dbContext.Set<TDestination>()` for unscoped behavior, or a pre-filtered queryset when the consumer carries its own scope.

The caller chooses the load shape — there is no "unscoped escape hatch" overload that silently defaults to `_dbContext.Set<TDestination>()`. A job that lacks a tenant context fails loudly (or scopes deliberately) at the call site rather than inheriting "all rows".

### View-layer extension points (`Perform*Async`)

`BaseController` exposes `protected virtual` hooks for the four mutating actions, mirroring DRF's `perform_create` / `perform_update` / `perform_destroy` at [`rest_framework/mixins.py`](https://github.com/encode/django-rest-framework/blob/3.17.1/rest_framework/mixins.py) (tag 3.17.1):

| Hook | Default | Mirrors |
|---|---|---|
| `PerformCreateAsync(data, ct)` | `_serializer.CreateAsync(data, ct)` | DRF `perform_create` (`mixins.py:22-23`) |
| `PerformUpdateAsync(instance, data, ct)` | `_serializer.UpdateAsync(instance, data, ct)` | DRF `perform_update` (`mixins.py:71-72`), PUT |
| `PerformPartialUpdateAsync(instance, data, ct)` | `_serializer.PartialUpdateAsync(instance, data, ct)` | DRF `perform_update` (`mixins.py:71-72`), PATCH |
| `PerformDestroyAsync(instance, ct)` | `_serializer.DestroyAsync(instance, ct)` | DRF `perform_destroy` (`mixins.py:88-89`) |

Filter-scoping has already happened at the action's load step before these hooks fire (see [Queryset scope on writes](#queryset-scope-on-writes-drf-parity)) — `instance` is guaranteed to be a row the caller is allowed to mutate. There is no `query` parameter to compose extra `Where` clauses onto; the hook is the place for instance-shaped predicates, tenant stamping, audit fields, transaction wrapping, and domain events.

Override these on the **controller** when the side effect is request-shaped — auth-derived audit fields, request-scoped tracing, or anything that touches `HttpContext`. Override the matching method on the **serializer** when the logic is shared with non-HTTP callers.

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PersonsController : BaseController<PersonDto, Person, int, AppDbContext>
{
    private readonly AppDbContext _dbContext;

    public PersonsController(
        Serializer<PersonDto, Person, int, AppDbContext> serializer,
        AppDbContext dbContext,
        ILogger<Person> logger
    ) : base(serializer, dbContext, logger)
    {
        _dbContext = dbContext;
    }

    protected override async Task<Person> PerformCreateAsync(
        PersonDto data, CancellationToken cancellationToken)
    {
        var created = await base.PerformCreateAsync(data, cancellationToken);
        // request-shaped side effect — write the caller's user id into an audit row.
        _dbContext.AuditLog.Add(new AuditEntry(created.Id, User.Identity?.Name));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    protected override async Task<Person> PerformUpdateAsync(
        Person instance, PersonDto data, CancellationToken cancellationToken)
    {
        // instance-shaped predicate — refuse the update if the loaded row is in a
        // state the caller can't transition out of.
        if (instance.IsLocked)
            throw new InvalidOperationException("Person is locked.");
        return await base.PerformUpdateAsync(instance, data, cancellationToken);
    }
}
```

`ValidateDestroyAsync` (described below) is a pre-delete *validation* seam — it short-circuits to `400` with a `ValidationErrors` envelope. `PerformDestroyAsync` is the *persistence* seam — wrap the delete in a transaction, publish a domain event, or write to an outbox there.

### Pre-delete validation (`ValidateDestroyAsync`)

`BaseController.ValidateDestroyAsync(instance, errors, ct)` runs after the entity is loaded and before `PerformDestroyAsync`. Populate `errors` to short-circuit the request with a `400` carrying the standard `ValidationErrors` envelope; use `ValidationErrors.NonFieldErrorsKey` for object-level rejections.

```csharp
protected override Task ValidateDestroyAsync(
    Store instance,
    IDictionary<string, List<string>> errors,
    CancellationToken cancellationToken)
{
    if (instance.HasOpenOrders)
        errors.GetOrAdd(ValidationErrors.NonFieldErrorsKey)
              .Add("Store has open orders and cannot be deleted.");
    return Task.CompletedTask;
}
```

This is the spot for state predicates that don't fit input validation. It is **validate-only** — keep transactions, outbox writes, and the authoritative re-check inside `PerformDestroyAsync` (on the controller) or `Serializer.DestroyAsync(TDestination, CancellationToken)` (on the serializer).

### Serializer

The `Serializer` handles DTO-to-entity conversion and database operations. It follows Django REST Framework's naming conventions for its core methods:

| Method | Description |
|---|---|
| `CreateAsync(data, ct)` | Persists a new entity. Called by `POST`. Mirrors DRF `ModelSerializer.create(validated_data)`. |
| `UpdateAsync(instance, origin, ct)` | Fully replaces the already-loaded `instance` in place. Mirrors DRF `ModelSerializer.update(instance, validated_data)` — the serializer is queryset-naive; the controller resolves the row via `GetObjectAsync(filteredQuery, id, ct)` before invoking this. |
| `PartialUpdateAsync(instance, origin, ct)` | Updates only the fields present in the request body on the already-loaded `instance`. Same loading contract as `UpdateAsync`. |
| `UpdateManyAsync(query, origin, entityIds, ct)` | Applies the same full update to every entity in `entityIds` that the supplied queryset admits. Bulk-execute path; headless-only. Pass `_dbContext.Set<TDestination>()` for unscoped behavior. |
| `GetObjectAsync(query, id, ct)` | Loads a single entity by id composed onto the supplied queryset. Mirrors DRF's `get_object`. The canonical lookup used by `GetSingle`, `Put`, `Patch`, and `Delete` to do filter-scoped resolution before handing the instance to the matching serializer write. |
| `DestroyAsync(instance, ct)` | Removes the already-loaded `instance`. Mirrors DRF's default `perform_destroy` body (`instance.delete()`). The override seam for delete-time side effects; the `DELETE /{id}` action loads via `GetObjectAsync` before calling this. |
| `DestroyManyAsync(query, entityIds, ct)` | Removes every entity in `entityIds` that the supplied queryset admits, using a single `ExecuteDeleteAsync` SQL statement; returns `Task` (no payload). Entities are not loaded into the change tracker. Called by the `DELETE ?ids=` action with the controller's filtered queryset. |

Every method accepts a `CancellationToken` (defaults to `default`); the controller flows
`HttpContext.RequestAborted` into it via MVC's `CancellationTokenModelBinder`, so a client
disconnect cooperatively cancels the in-flight DB work. Forward the token to your own EF
calls when you override.

The default serializer works for most cases. Override any method for custom logic:

```csharp
public class PersonSerializer : Serializer<PersonDto, Person, int, AppDbContext>
{
    public PersonSerializer(AppDbContext context) : base(context) { }

    public override async Task<Person> CreateAsync(
        PersonDto data,
        CancellationToken cancellationToken = default)
    {
        // Custom logic before/after creation
        var result = await base.CreateAsync(data, cancellationToken);
        return result;
    }
}
```

Register the custom serializer instead of the base one:

```csharp
builder.Services.AddScoped<Serializer<PersonDto, Person, int, AppDbContext>, PersonSerializer>();
```

### Validation

Validation happens at two layers:

1. **DataAnnotations on DTOs** -- Standard `[Required]`, `[MinLength]`, `[Range]`, etc. attributes on your DTO properties. These are evaluated during model binding (before the controller action runs) and produce `ValidationErrors` responses (wired up automatically by `AddNDjangoRestFramework()`).

2. **Serializer hooks** -- For rules that need async I/O (database uniqueness checks), cross-field logic, or DTO mutation (normalizing values before persistence). Define hooks on your serializer subclass (see next section).

```csharp
using System.ComponentModel.DataAnnotations;
using NDjango.RestFramework.Base;

public class PersonDto : BaseDto<int>
{
    [MinLength(3, ErrorMessage = "Name should have at least 3 characters")]
    public string Name { get; set; }
}
```

### Custom validation and normalization

The serializer runs a two-stage pipeline (inspired by DRF's `validate_<field>` + `validate()`):

1. **Per-field hooks** -- Methods named `Validate{PropertyName}Async` on your serializer. Auto-discovered by convention, invoked for POST/PUT/PATCH. For PATCH, a hook is only called if the field was present in the payload. Return the (possibly normalized) value.
2. **Cross-field hook** -- Override `ValidateAsync(data, context, errors, cancellationToken)`. Runs **only if all per-field hooks passed** (no errors), so you can safely assume individual fields are valid.

Both stages receive a `ValidationContext<TPrimaryKey>` carrying `Operation` (`Create` / `Update` / `PartialUpdate` / `BulkUpdate`) and `EntityId`. Populate `errors` to signal failures; the controller returns `400 ValidationErrors` as soon as the dictionary is non-empty.

Example -- CNPJ normalization + uniqueness + a cross-field rule, no duplication across POST/PUT/PATCH:

```csharp
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;

public class StoreSerializer : Serializer<StoreDto, Store, Guid, AppDbContext>
{
    public StoreSerializer(AppDbContext context) : base(context) { }

    // Per-field hook: runs for POST, PUT, and PATCH (PATCH only if CNPJ was sent).
    public async Task<string> ValidateCNPJAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (value == null) return value;

        // Normalize: strip non-digits. The returned value is written back automatically.
        var cnpj = Regex.Replace(value, @"\D", "");

        if (cnpj.Length != 14)
            errors.GetOrAdd("CNPJ").Add("CNPJ must have 14 digits.");

        if (cnpj.Length == 14)
        {
            var query = _dbContext.Store.AsNoTracking().Where(s => s.CNPJ == cnpj);
            if (!context.IsCreate)
                query = query.Where(s => s.Id != context.EntityId); // skip-self
            if (await query.AnyAsync(cancellationToken))
                errors.GetOrAdd("CNPJ").Add("Store with this CNPJ already exists.");
        }

        return cnpj;
    }

    // Cross-field hook: runs only if per-field hooks added no errors.
    public override Task<StoreDto> ValidateAsync(
        StoreDto data,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(data.Name) && data.Name == data.CNPJ)
            errors.GetOrAdd("Name").Add("Name cannot be the same as CNPJ.");

        return Task.FromResult(data);
    }
}
```

Register the custom serializer in DI:

```csharp
builder.Services.AddScoped<StoreSerializer>();
```

The `GetOrAdd` extension method is a **public** API in `NDjango.RestFramework.Helpers`. Add the following `using` to your serializer file:

```csharp
using NDjango.RestFramework.Helpers;
```

#### Hook conventions

- **Method name**: `Validate{PropertyName}Async`, where `PropertyName` matches a property on your DTO exactly (case-insensitive). Misnamed hooks (e.g., `ValidateCnjAsync` when the DTO has `CNPJ`) are caught at startup by the hosted service that `AddNDjangoRestFramework()` registers.
- **Signature**: `Task<TFieldType> Validate{Property}Async(TFieldType value, ValidationContext<TPrimaryKey> context, IDictionary<string, List<string>> errors, CancellationToken cancellationToken = default)`. The return type must match the property type. Forward the token to any EF call inside the hook (e.g., `AnyAsync(cancellationToken)`); reflection discovery only matches this exact 4-parameter shape, so older 3-parameter hooks will silently not run.
- **Mutation**: return a different value from the hook and the framework writes it back (onto the DTO for POST/PUT, onto the `PartialJsonObject<T>` for PATCH).
- **Branching on operation**: use `context.Operation`, `context.IsCreate`, `context.IsUpdate`, `context.IsPartialUpdate`, or `context.IsBulkUpdate`. Avoid deriving intent from `EntityId == default` — it's ambiguous for bulk updates and for `int` primary keys.

#### Guidelines

- **Use `.AsNoTracking()` on any DB reads** inside validation. Validation should never track entities.
- **Never call `SaveChangesAsync()`** inside validation. The base CRUD method (`CreateAsync`, `UpdateAsync`, etc.) saves after validation succeeds.
- **Do not attach, add, or remove entities during validation** -- only read.
- **The `errors` dictionary and `PartialJsonObject<T>` are not thread-safe.** Do not share them across `Task.WhenAll` subtasks without external synchronization.
- **`PartialJsonObject.SetValue` only supports top-level properties** for absent fields. If the property path is nested (e.g., `d => d.Address.Street`) and the path is not present in the incoming JSON, `SetValue` throws `NotSupportedException`. Nested paths that already exist in the JSON can be replaced normally. (The framework only uses `SetValue` internally for properties it knows are set, so this limitation only affects code that calls `SetValue` directly.)
- **`BulkUpdate` operations have no single `EntityId`.** Hook authors needing per-entity validation context should override `UpdateManyAsync` and perform those checks there before the bulk update.

#### Branching with `ValidationContext`

The `ValidationContext<TPrimaryKey>` passed to every hook exposes:

| Member | Purpose |
|---|---|
| `Operation` | `SerializerOperation` enum: `Create`, `Update`, `PartialUpdate`, `BulkUpdate`. |
| `IsCreate` / `IsUpdate` / `IsPartialUpdate` / `IsBulkUpdate` | Sugar over `Operation`. Prefer these to checking `EntityId == default`. |
| `EntityId` | The target entity's id for `Update` / `PartialUpdate`. `default` for `Create` and `BulkUpdate`. |
| `IsSet(string fieldName)` | On `PartialUpdate`, `true` only if the field was present in the PATCH body. On `Create`/`Update`/`BulkUpdate`, always `true` (DRF semantics: full payloads imply every field was sent). Useful in cross-field rules that should treat absent PATCH fields as "no opinion." |

#### Applying a partial body onto a loaded entity

`PartialJsonObject<T>.ApplyTo(entity, except: ...)` copies every present field onto a target object, silently skipping mismatched names, missing setters, and incompatible types. Useful when you override `PartialUpdateAsync` and want to bypass the default reflection-based copy:

```csharp
public override async Task<Person> PartialUpdateAsync(
    Person instance,
    PartialJsonObject<PersonDto> dto,
    CancellationToken cancellationToken = default)
{
    // The controller has already loaded `instance` through its filter chain
    // (see "Queryset scope on writes"); the serializer is queryset-naive.
    dto.ApplyTo(instance, except: nameof(Person.CreatedAt));

    await _dbContext.SaveChangesAsync(cancellationToken);
    return instance;
}
```

### Using `Serializer<>` outside an HTTP context

`Serializer<TOrigin, TDestination, TPrimaryKey, TContext>` depends only on a `DbContext` — there is no `HttpContext` reference on the serializer's public surface. `RunValidationAsync` and the CRUD methods (`CreateAsync`, `UpdateAsync`, `PartialUpdateAsync`, `UpdateManyAsync`, `DestroyAsync`, `DestroyManyAsync`) are part of the public API, so a message consumer, gRPC service, or scheduled job can reuse the same serializer the controller uses, with the same per-field hooks, cross-field rules, and PATCH presence semantics.

Register your serializer subclass with the same lifetime as the `DbContext` it depends on (typically Scoped — one serializer per HTTP request, message, or job). A serializer is shared mutable state by construction (`_dbContext`, plus any state your subclass carries) and must not be invoked concurrently on a single instance; resolving one per logical unit of work matches both EF Core's contract and the way `BaseController` already uses it.

A consumer-shaped example:

```csharp
public class CreatePersonHandler
{
    private readonly PersonSerializer _serializer;

    public CreatePersonHandler(PersonSerializer serializer) => _serializer = serializer;

    public async Task HandleAsync(PersonDto message, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var context = new ValidationContext<int>(SerializerOperation.Create, default);

        await _serializer.RunValidationAsync(message, context, errors, cancellationToken: cancellationToken);
        if (errors.Count > 0)
        {
            // Inspect, log, dead-letter — your call.
            return;
        }

        await _serializer.CreateAsync(message, cancellationToken);
    }
}
```

For partial updates from a raw JSON payload, construct a `PartialJsonObject<T>` directly with the body string and pass it to both validation and `PartialUpdateAsync`. The headless caller loads the instance — using whatever row-scoping predicate it carries — and hands it to the serializer:

```csharp
var partial = new PartialJsonObject<PersonDto>(rawJsonString);
var errors = new Dictionary<string, List<string>>();
var context = new ValidationContext<int>(SerializerOperation.PartialUpdate, entityId, partial);

await _serializer.RunValidationAsync(partial.Instance, context, errors, partial, cancellationToken);
if (errors.Count == 0)
{
    var instance = await _dbContext.Set<Person>()
        .Where(p => p.Tenant == currentTenant) // or .Set<Person>() for unscoped
        .FirstOrDefaultAsync(p => p.Id == entityId, cancellationToken);
    if (instance is not null)
        await _serializer.PartialUpdateAsync(instance, partial, cancellationToken);
}
```

#### When the DTO uses `System.Text.Json` attributes

`CreateAsync`, `UpdateAsync`, and `UpdateManyAsync` map DTO → entity through a Newtonsoft round-trip by default. If your DTO is decorated with `[JsonPropertyName]` (System.Text.Json) instead of `[JsonProperty]` (Newtonsoft) — common in messaging stacks — the rename is invisible to the round-trip and the field silently fails to map. Override the mapping seams to do explicit, attribute-free copying:

```csharp
public class PersonSerializer : Serializer<PersonDto, Person, int, AppDbContext>
{
    public PersonSerializer(AppDbContext context) : base(context) { }

    protected override Person MapToDestination(PersonDto origin) => new()
    {
        Name = origin.Name,
    };

    protected override void ApplyToDestination(PersonDto origin, Person destination, int entityId)
    {
        destination.Id = entityId;
        destination.Name = origin.Name;
    }
}
```

When you override these seams the default round-trip does not run, so your override owns all property and navigation copying — including preserving the entity's primary key in `ApplyToDestination`.

> **Transactions and side effects.** Transactions, outbox writes, and message publishing are intentionally not modeled inside the serializer. Compose them in the caller (the controller, the consumer, the job) so each call site can pick its own transaction boundary and decide whether to publish.

### Error handling

`BaseController` does **not** catch exceptions. Unhandled exceptions propagate to the ASP.NET Core middleware pipeline, where the host application can handle them using `IExceptionHandler` / `app.UseExceptionHandler()`. This gives the host full control over status codes, error shapes, logging severity, and observability enrichment.

The library produces two structured error responses:

- **ValidationErrors** — When model state validation fails or a serializer hook populates the `errors` dictionary (wired automatically by `AddNDjangoRestFramework()`):
  ```json
  {"type": "VALIDATION_ERRORS", "statusCode": 400, "error": {"Name": ["Name should have at least 3 characters"]}}
  ```

- **UnexpectedError** — Returned only for library-level configuration errors (e.g., `GetFields()` referencing invalid properties):
  ```json
  {"type": "UNEXPECTED_ERROR", "statusCode": 500, "error": {"msg": "..."}}
  ```

For domain exceptions, infrastructure failures, and all other error scenarios, the host's exception middleware is responsible for producing the appropriate response.

## Notice

This project is still in the early stages of development. We recommend that you do not use it in production environments and check the written tests to understand the current functionality.
