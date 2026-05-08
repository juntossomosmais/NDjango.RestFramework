---
name: IsAction visibility filter on net8.0
description: DefaultApplicationModelProvider.IsAction excludes non-public, abstract, generic, static, special-name, and IDisposable methods — so protected virtual hooks on a controller are never routed
type: reference
---

`Microsoft.AspNetCore.Mvc.ApplicationModels.DefaultApplicationModelProvider.IsAction(TypeInfo, MethodInfo)` is the authoritative gate for whether a controller method becomes a discoverable MVC action. On `release/8.0`, `src/Mvc/Mvc.Core/src/ApplicationModels/DefaultApplicationModelProvider.cs` (around lines 360–410), it returns false for any of:

- `IsSpecialName` (property accessors, operators)
- decorated with `[NonAction]`
- declared by `System.Object`
- `IDisposable.Dispose` (via `IsIDisposableMethod`)
- `IsStatic`
- `IsAbstract`
- `IsConstructor`
- `IsGenericMethod`

…and finally returns `methodInfo.IsPublic`.

Practical consequence for this project:

- `protected virtual Task<TDestination> PerformCreateAsync(...)` and similar hooks on `BaseController<...>` will NOT be discovered as routable actions, even if a derived controller carries `[ApiController]`. No `[NonAction]` annotation needed.
- Same protection covers `protected virtual Task ValidateDestroyAsync(...)`.
- `[NonAction]` is still required (and is present) on `public virtual` helper methods like `FilterQuery`, `SortQuery`, `GetQuerySet` because they ARE public — visibility alone wouldn't exclude them.

Re-verify if the project moves off net8.0; the `IsAction` body has been stable since at least 6.0 but a later major could relax it.
