# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is this?

NDjango.RestFramework is a .NET 8 library that provides Django REST Framework-inspired CRUD API patterns for ASP.NET Core. It reduces boilerplate for building REST APIs by providing generic base classes for controllers, serializers, filters, and pagination. Published as a NuGet package (`NDjango.RestFramework`).

## Architecture

The framework is built on a chain of generics: `<TOrigin, TDestination, TPrimaryKey, TContext>` where TOrigin=DTO, TDestination=Entity, TPrimaryKey=ID type (Guid, int, etc.), TContext=DbContext.

### Core pipeline

`BaseController` → `Serializer` → `DbContext`

- **BaseController** (`src/NDjango.RestFramework/Base/BaseController.cs`) — Provides GET, POST, PUT, PATCH, DELETE endpoints (plus bulk `PUT ?ids=` and `DELETE ?ids=`). Orchestrates filtering, sorting, pagination, and field selection. Actions can be toggled via `ActionOptions`. Does not catch exceptions — host is expected to wire `IExceptionHandler` / `UseExceptionHandler()`.
- **Serializer** (`src/NDjango.RestFramework/Serializer/Serializer.cs`) — Converts DTOs ↔ entities and runs DB operations. DRF-style method names: `CreateAsync`, `UpdateAsync`, `PartialUpdateAsync`, `UpdateManyAsync`, `DestroyAsync`, `DestroyManyAsync`. Validation is a DRF-style pipeline: define per-field hooks `Validate{PropertyName}Async(value, ValidationContext<TPrimaryKey>, errors)` that auto-discover by convention and run for POST/PUT/PATCH (PATCH skips absent fields). `ValidationContext.Operation` exposes a `SerializerOperation` enum (`Create`/`Update`/`PartialUpdate`/`BulkUpdate`) so hooks can branch on intent. After per-field hooks, cross-field `ValidateAsync(data, context, errors)` runs only if no errors. Legacy overloads (`ValidateAsync(data, errors)`, `(data, id, errors)`, `(partial, id, errors)`) remain for backward compat. Override any method for custom logic and register the subclass in DI.
- **PartialJsonObject<T>** (`src/NDjango.RestFramework/Helpers/PartialJsonObject.cs`) — Tracks which fields were actually present in a PATCH body so absent fields stay untouched.
- **JsonTransform** (`src/NDjango.RestFramework/Serializer/JsonTransform.cs`) — Custom Newtonsoft.Json contract resolver that filters serialized fields based on `BaseModel.GetFields()`. Nested field selection uses `"ClassName:FieldName"` syntax.

### Filters (applied sequentially via `BaseController.Filters`)

Each filter receives the `IQueryable` from the previous one, so `Filter<TEntity>` is also the extension point for `.Include()`, conditional joins, or any other query shaping — not just filtering.

- **QueryStringFilter** (`Filters/QueryStringFilter.cs`) — Exact match on query params mapped to entity fields. Only fields in `AllowedFields` are honored.
- **QueryStringSearchFilter** (`Filters/QueryStringSearchFilter.cs`) — Multi-field `LIKE` search via `EF.Functions.Like()`, combined with OR.
- **QueryStringIdRangeFilter** (`Filters/QueryStringIdRangeFilter.cs`) — Filter by `ids=1,2,3` or `ids=1&ids=2&ids=3`.
- **SortFilter** (`Filters/SortFilter.cs`) — Dynamic `sort` / `sortDesc` driven by reflection + expression trees.

### Pagination

- **IPagination** (`Paginations/Pagination.cs`) — Receives `IQueryable` + `HttpRequest`, returns `Paginated<T>` (count/next/previous/results).
- **PageNumberPagination** (`Paginations/PageNumberPagination.cs`) — Django-style `?page=1&page_size=10` (default `page_size=5`, max 50). Swap by passing a custom `IPagination<TDestination>` to the `BaseController` constructor.

### Startup validation

`builder.Services.ValidateControllerFieldsOnStartup()` (via `Extensions/ControllerFieldValidationExtensions.cs` + `Validation/ControllerFieldValidationHostedService.cs`) asserts at app startup that every name in `GetFields()` and `AllowedFields` resolves to a real property on the entity, and that every `Validate{X}Async` hook on a serializer maps to a real DTO property (catches typos like `ValidateCnjAsync` vs `ValidateCnpjAsync`). Misconfigured controllers fail fast instead of 500'ing at request time.

### Error responses

Only two structured shapes are produced by the library:

- `ValidationErrors` (`Errors/ValidationErrors.cs`) — emitted when `ConfigureValidationResponseFormat()` is registered and model state fails, or when a `ValidateAsync` override populates its `errors` dictionary. Shape: `{ "type": "VALIDATION_ERRORS", "statusCode": 400, "error": {...} }`.
- `UnexpectedError` (`Errors/UnexpectedError.cs`) — library-level configuration errors only (e.g., bad `GetFields()`). Shape: `{ "type": "UNEXPECTED_ERROR", "statusCode": 500, "error": {"msg": "..."} }`.

Anything else is the host's responsibility.

## Tests

- Project: `tests/NDjango.RestFramework.Test/` (net8.0, xUnit, Moq, Moq.AutoMock, Bogus).
- Test harness: `Support/FakeProgram.cs` + `Support/IntegrationTestsFixture.cs` boot an in-process ASP.NET host backed by the real SQL Server container; each test class gets its own database (the connection string in `docker-compose.yml` has `REPLACE_ME_PROGRAMATICALLY` that is rewritten per fixture).
- Shared test doubles live under `Support/` (`Controllers.cs`, `Serializers.cs`, `Filters.cs`, `Models.cs`, `DTOs.cs`, etc.) — reuse these before inventing new ones.
- The test project has `InternalsVisibleTo` to the main assembly, so `internal` APIs are reachable from tests.
