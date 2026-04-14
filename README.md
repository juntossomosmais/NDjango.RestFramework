# NDjango.RestFramework

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
| `PUT`    | `/api/v1/Persons?ids=`  | Bulk full update               |
| `PATCH`  | `/api/v1/Persons/{id}`  | Partial update                 |
| `DELETE` | `/api/v1/Persons/{id}`  | Delete                         |
| `DELETE` | `/api/v1/Persons?ids=`  | Bulk delete                    |

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

Register Newtonsoft.Json (required), the validation response format, and each serializer:

```csharp
using NDjango.RestFramework.Extensions;
using NDjango.RestFramework.Serializer;
using Newtonsoft.Json;

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("AppDbContext")));

// Controllers + Newtonsoft.Json + validation format
builder.Services.AddControllers()
    .AddNewtonsoftJson(config =>
    {
        config.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        config.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    })
    .ConfigureValidationResponseFormat();

// Register one serializer per controller
builder.Services.AddScoped<Serializer<PersonDto, Person, int, AppDbContext>>();
builder.Services.AddScoped<Serializer<TodoItemDto, TodoItem, int, AppDbContext>>();

// Validate controller field configuration at startup (recommended)
builder.Services.ValidateControllerFieldsOnStartup();
```

`AddNewtonsoftJson` is required because the library uses Newtonsoft.Json internally for serialization and field filtering. `ConfigureValidationResponseFormat()` ensures validation errors return a structured `ValidationErrors` response. `ValidateControllerFieldsOnStartup()` checks that all field names in `GetFields()` and `AllowedFields` reference actual properties on the entity — the application will fail to start if any controller is misconfigured.

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

Use `ActionOptions` to disable PUT or PATCH:

```csharp
public PersonsController(...)
    : base(serializer, context, new ActionOptions { AllowPatch = false }, logger)
{ }
```

Disabled endpoints return `405 Method Not Allowed`.

### Serializer

The `Serializer` handles DTO-to-entity conversion and database operations. The default serializer works for most cases. Override it for custom logic:

```csharp
public class PersonSerializer : Serializer<PersonDto, Person, int, AppDbContext>
{
    public PersonSerializer(AppDbContext context) : base(context) { }

    public override async Task<Person> PostAsync(PersonDto data)
    {
        // Custom logic before/after creation
        var result = await base.PostAsync(data);
        return result;
    }
}
```

Register the custom serializer instead of the base one:

```csharp
builder.Services.AddScoped<Serializer<PersonDto, Person, int, AppDbContext>, PersonSerializer>();
```

### Validation

Use [FluentValidation](https://docs.fluentvalidation.net/) for DTO validation. The `IHttpContextAccessor` lets you apply rules conditionally based on the HTTP method:

```csharp
public class PersonDtoValidator : AbstractValidator<PersonDto>
{
    public PersonDtoValidator(IHttpContextAccessor context)
    {
        RuleFor(m => m.Name)
            .MinimumLength(3)
            .WithMessage("Name should have at least 3 characters");

        if (context.HttpContext.Request.Method == HttpMethods.Post)
            RuleFor(m => m.Name)
                .NotEmpty()
                .WithMessage("Name is required");
    }
}
```

Make sure to register `AddHttpContextAccessor()` in your `Program.cs` if you use `IHttpContextAccessor` in validators:

```csharp
builder.Services.AddHttpContextAccessor();
```

### Error handling

`BaseController` does **not** catch exceptions. Unhandled exceptions propagate to the ASP.NET Core middleware pipeline, where the host application can handle them using `IExceptionHandler` / `app.UseExceptionHandler()`. This gives the host full control over status codes, error shapes, logging severity, and observability enrichment.

The library produces two structured error responses:

- **ValidationErrors** — When model state validation fails (requires `ConfigureValidationResponseFormat()`):
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
