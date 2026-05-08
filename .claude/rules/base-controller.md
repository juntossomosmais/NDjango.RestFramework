---
paths: "src/NDjango.RestFramework/Base/BaseController.cs"
---

# BaseController

C# port of DRF's `ModelViewSet` (`encode/django-rest-framework@3.17.1`). Diverge only when forced by C#/ASP.NET Core idioms; document the divergence.

## Action set

- Ship exactly: `GetSingle`, `ListPaged`, `Post`, `Patch`, `Put`, `Delete`, `DeleteMany`. Maps to DRF `retrieve`/`list`/`create`/`partial_update`/`update`/`destroy` plus opt-in bulk delete.
- Do not add `PutMany`, `PostMany`, `PatchMany`, or any bulk-mutation-over-HTTP action. DRF rejects at `serializers.py:734`; the rationale extends to every mutating verb.
- Do not collapse `Patch` and `Put`. DRF *does* collapse (`mixins.py:80`); we diverge because `PartialJsonObject<TOrigin>` carries an IsSet mask `TOrigin` cannot.
- Action names diverge from DRF (`GetSingle` vs `retrieve`, `ListPaged` vs `list`) for ASP.NET routing. Semantics match, names do not. Do not rename to DRF names.

## Filter scoping

- Build `var query = FilterQuery(GetQuerySet(), HttpContext.Request);` at the start of every action that resolves an id: `GetSingle`, `Patch`, `Put`, `Delete`, `DeleteMany`. Mirrors `generics.py:79-102`.
- Load via `_serializer.GetObjectAsync(query, id, ct)` for every single-row write action. Never `Set<T>().FindAsync` or any unscoped path — cross-tenant mutation gap.
- Return `NotFound()` immediately when the load returns `null`. Out-of-scope id and missing id must be indistinguishable.

## `Perform*Async` hooks

- Define each hook as `protected virtual`. The default body forwards to the matching serializer method with no transformation — a subclass calling `base.Perform*Async(...)` must get the default behavior.
- Use these signatures:
  - `PerformCreateAsync(TOrigin data, CT)` → `_serializer.CreateAsync(data, ct)`. `mixins.py:23`.
  - `PerformUpdateAsync(TDestination instance, TOrigin data, CT)` → `_serializer.UpdateAsync(instance, data, ct)`. `mixins.py:77`.
  - `PerformPartialUpdateAsync(TDestination instance, PartialJsonObject<TOrigin> data, CT)` → `_serializer.PartialUpdateAsync(instance, data, ct)`. PATCH companion; `mixins.py:80`.
  - `PerformDestroyAsync(TDestination instance, CT)` → `_serializer.DestroyAsync(instance, ct)`. `mixins.py:94`.
- Do not add `IQueryable<>`, `TPrimaryKey id`, or any scoping parameter to a hook. Filter scoping happened upstream at the load step.
- Route every mutation in `Post`/`Put`/`Patch`/`Delete` through the matching hook. Custom actions on consumer subclasses may call the serializer directly but should expose their own `Perform{Custom}Async`.
- Do not add controller-level hooks for actions without a DRF analogue (`PerformGetSingleAsync`, `PerformListAsync`).

## `ValidateDestroyAsync`

- Reserve `ValidateDestroyAsync(instance, errors, ct)` for pre-delete validation. Collect errors; short-circuit `400 BadRequest` if any.
- Do not perform side effects. No EF writes, no domain events. Override `Serializer.DestroyAsync(instance)` or `PerformDestroyAsync` for those.
- Do not revert the rename. DRF's `perform_destroy` is a delete-site override; this is a validation seam.

## `DeleteMany`

- Default `ActionOptions.AllowBulkDelete = false`. Opt-in only.
- Implement via `_serializer.DestroyManyAsync(query, ids, ct)`. Return `204 NoContent`. Never loop-load-then-delete.
- Document that the bulk path bypasses `ValidateDestroyAsync`, `PerformDestroyAsync`, and any per-row `Serializer.DestroyAsync` override.

## Validation flow

- Call `_serializer.RunValidationAsync(entity, context, errors, ...)` at the start of every mutating action with the correct `SerializerOperation`. No `BulkUpdate` operation exists.
- Return `400 BadRequest(new ValidationErrors(...))` when `errors.Count > 0`. Do not integrate with `ModelState`.

## Response shape

- Serialize via `JsonConvert.SerializeObject(..., new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) })`.
- Return `CreatedAtAction(nameof(GetSingle), new { id = data.Id }, jObject)` from `Post`. Named action references only.
- Return `204 NoContent()` from `Delete` and `DeleteMany`. Do not return the deleted entity.
- Return `405 MethodNotAllowed` when an action is disabled by `ActionOptions`.

## Generics

- Do not add a fifth type parameter to `BaseController`. The chain `<TOrigin, TDestination, TPrimaryKey, TContext>` is the porting contract.

## XML docs

- Cite DRF source on every action and hook as `mirrors DRF's <c>X.y</c> at <c>file.py:L-L</c> of <c>encode/django-rest-framework@3.17.1</c>`.
- State the security guarantee on every row-touching action: composes `Filters` over the queryset before resolving the id; out-of-scope rows return 404.

## Forbidden

- Do not bypass `Perform*Async` to call the serializer directly from the canonical actions.
- Do not add actions to match external framework conventions (`Replace`, `Upsert`, `BatchPatch`).
- Do not inject `IHttpContextAccessor` — `HttpContext` is on `ControllerBase`.
