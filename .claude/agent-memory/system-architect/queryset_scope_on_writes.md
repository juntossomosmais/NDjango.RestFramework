---
name: Queryset scope on writes
description: Controller's Filters chain composes into every write action's load step; hooks receive the already-loaded instance, not a queryable.
type: project
---

Every action that resolves an id to an entity (GetSingle, Put, Patch, Delete, DeleteMany) builds `var query = FilterQuery(GetQuerySet(), HttpContext.Request);` and applies it once: the controller calls `_serializer.GetObjectAsync(query, id, ct)` before invoking the matching `Perform*Async` hook. Hooks receive `(TDestination instance, ...)` — DRF parity with `ModelSerializer.update(self, instance, validated_data)` (`mixins.py:58-67` at tag 3.17.1). The serializer's single-row write methods (`UpdateAsync`, `PartialUpdateAsync`, `DestroyAsync`) also take a pre-loaded instance and are queryset-naive. Only bulk-execute primitives (`UpdateManyAsync`, `DestroyManyAsync`, `GetManyFromDB`) carry `IQueryable<TDestination>` — forced by EF Core's set-based `ExecuteDeleteAsync`/`ExecuteUpdateAsync`.

**Why:** DRF parity — `GenericAPIView.get_object()` calls `filter_queryset(get_queryset())` before lookup so a tenant filter or soft-delete filter protects writes the same way it protects reads. Out-of-scope id resolves to null → 404, no information leak about cross-tenant existence. The instance-taking surface matches DRF's queryset-naive serializer contract, so consumers familiar with DRF know exactly where to put load-vs-mutate boundaries.

**How to apply:**
- Controller load step is the *only* place queryset scoping happens for single-row writes. Don't re-add `IQueryable` parameters to `Perform{Create,Update,PartialUpdate,Destroy}Async` or to `Serializer.{Update,PartialUpdate,Destroy}Async` — the queryset-naive serializer is a hard invariant pinned by `.claude/rules/serializer.md`.
- Bulk paths (`DestroyManyAsync`, `UpdateManyAsync`) take `IQueryable<TDestination>` because `ExecuteDeleteAsync` operates on a queryable. Document the bypass on every bulk method: per-row `ValidateDestroyAsync`, controller `PerformDestroyAsync` override, serializer `DestroyAsync` override, EF interceptors, soft-delete logic — none of these fire on the bulk path.
- Headless callers: pass `_dbContext.Set<TDestination>()` as the query for unscoped bulk operations; pass a tracking instance loaded any way the caller chooses for single-row writes.
- `GetObjectAsync(query, id, ct)` uses `query.Where(x => x.Id.ToString() == key)` — tracking by default. Read paths that want no-tracking must compose `.AsNoTracking()` into the query before handing it to the serializer; the serializer does not inject it.
- Generics chain `<TOrigin, TDestination, TPrimaryKey, TContext>` is unchanged; the load-then-mutate split fits inside it cleanly.
