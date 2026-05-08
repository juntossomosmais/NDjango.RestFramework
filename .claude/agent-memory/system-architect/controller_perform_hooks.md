---
name: Controller perform_* hook surface
description: BaseController exposes Perform{Create,Update,PartialUpdate,Destroy}Async action seams plus ValidateDestroyAsync (validation-only); naming is dual-prefix
type: project
---

`BaseController` (in `src/NDjango.RestFramework/Base/BaseController.cs`) exposes:

- `PerformCreateAsync(TOrigin data, ct) -> Task<TDestination>` — defaults to `_serializer.CreateAsync(data, ct)`
- `PerformUpdateAsync(TDestination instance, TOrigin data, ct) -> Task<TDestination>` — defaults to `_serializer.UpdateAsync(instance, data, ct)`
- `PerformPartialUpdateAsync(TDestination instance, PartialJsonObject<TOrigin> data, ct) -> Task<TDestination>` — defaults to `_serializer.PartialUpdateAsync(instance, data, ct)`
- `PerformDestroyAsync(TDestination instance, ct) -> Task` — defaults to `_serializer.DestroyAsync(instance, ct)`
- `ValidateDestroyAsync(TDestination, errors, ct) -> Task` — pre-delete validation seam; populating `errors` short-circuits to 400. NOT a DRF `perform_destroy` analogue.

**Hook surface is instance-taking on the write hooks**: Update / PartialUpdate / Destroy receive the already-loaded `TDestination` from the controller's filter-scoped load step. Hooks do not see `IQueryable<>` or `TPrimaryKey id` — row-scoping happened upstream.

**Naming is intentionally dual-prefix**: `Perform*` for action seams (transaction/audit wrappers around persistence, no error envelope), `Validate*` for validation-only seams that contribute to the `errors` envelope. The name `ValidateDestroyAsync` was chosen instead of `PerformDestroyAsync` precisely to keep this separation — `Perform*` must never carry `errors`.

**Why:** Without `Perform*` action hooks, consumers had to override the action method (losing validation/JSON-render plumbing) or override the serializer's CRUD methods (conflating view and data layers). Controller hooks give a request-shaped seam where `HttpContext.User` and request headers are in scope.

**How to apply:**

- Request-shaped side effects (audit from JWT, tenant stamping, outbox dispatch on the request thread, transaction wrappers that need `HttpContext`) → controller `Perform*Async` hook overrides.
- Logic shared with non-HTTP callers (message consumers, jobs) → serializer's CRUD method overrides (`CreateAsync` / `UpdateAsync` / `PartialUpdateAsync` / `DestroyAsync`).
- Gate deletes by entity state (open orders, dependents) → `ValidateDestroyAsync`.
- Instance-shaped predicates that depend on the loaded row's state (e.g., `X-Region` header matches `instance.Region`) → `PerformUpdateAsync` / `PerformPartialUpdateAsync` override that inspects `instance` before delegating to `base`. The `RegionGuardedCustomersController` in tests pins this pattern.
