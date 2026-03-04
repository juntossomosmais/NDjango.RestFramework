# NDjango.RestFramework

## What is this?

NDjango.RestFramework is a .NET 8 NuGet library that provides Django REST Framework-inspired CRUD API patterns for ASP.NET Core. It reduces boilerplate for building REST APIs by providing generic base classes for controllers, serializers, filters, and pagination.

## Commands

**All commands must run inside the Docker container.** Never run `dotnet build` directly.

```shell
# Run all tests (build + test)
docker compose down --remove-orphans -t 0 && docker compose run --rm --remove-orphans integration-tests 2>&1 | tail -100

# Run a single test by fully qualified name
docker compose run --rm --remove-orphans integration-tests dotnet test tests/NDjango.RestFramework.Test --configuration Release --filter "FullyQualifiedName~ClassName.MethodName"

# Run formatter (run after all tests pass)
docker compose run --rm --remove-orphans lint-formatter

# Run any arbitrary dotnet command
docker compose run --remove-orphans --rm integration-tests dotnet <command>
```

Coverage reports are written to `./tests-reports/[report-id]/` (mapped from `/app/tests-reports/` in the container).

## Architecture

The framework is built on a chain of generics: `<TOrigin, TDestination, TPrimaryKey, TContext>` where TOrigin=DTO, TDestination=Entity, TPrimaryKey=ID type (Guid, int, etc.), TContext=DbContext.

### Core pipeline

`BaseController` → `Serializer` → `DbContext`

- **BaseController** (`Base/BaseController.cs`) — Provides GET, POST, PUT, PATCH, DELETE endpoints. Orchestrates filtering, sorting, pagination, and field selection. Actions can be toggled via `ActionOptions`.
- **Serializer** (`Serializer/Serializer.cs`) — Converts between DTOs and entities, handles DB operations (create, update, patch, delete). PATCH uses `PartialJsonObject<T>` to detect which fields were actually sent in the request body.
- **JsonTransform** (`Serializer/JsonTransform.cs`) — Custom Newtonsoft.Json contract resolver that filters serialized fields based on `BaseModel.GetFields()`. Supports nested field selection via `"ClassName:FieldName"` syntax.

### Filters (applied sequentially via `BaseController.Filters`)

- **QueryStringFilter** (`Filters/QueryStringFilter.cs`) — Exact match on query params mapped to entity fields.
- **QueryStringSearchFilter** (`Filters/QueryStringSearchFilter.cs`) — Multi-field LIKE search using `EF.Functions.Like()`, combines with OR.
- **QueryStringIdRangeFilter** (`Filters/QueryStringIdRangeFilter.cs`) — Filter by `ids=id1,id2,id3` query param.
- **SortFilter** (`Filters/SortFilter.cs`) — Dynamic sorting via `sort`/`sortDesc` query params using reflection + expression trees.

### Pagination

- **IPagination** (`Paginations/Pagination.cs`) — Interface. Implementations receive `IQueryable` and `HttpRequest`, return `Paginated<T>` with count/next/previous/results.
- **PageNumberPagination** (`Paginations/PageNumberPagination.cs`) — Django-style `?page=1&page_size=10`.

### Test infrastructure

Tests use `WebApplicationFactory<FakeProgram>` for integration testing against a real SQL Server (via Docker Compose). Test support lives in `tests/.../Support/` with fake controllers, models, DTOs, serializers, validators, and a Bogus-based data generator.

## Data access rules

- Always add `.AsNoTracking()` on read-only queries.
- Add `.AsSplitQuery()` when a query has multiple collection `.Include()` chains.
- Use `ExecuteDeleteAsync()` / `ExecuteUpdateAsync()` for bulk operations.
