---
name: divergence_iqueryable_serializer_surface
description: Single-row writes now instance-taking (DRF-shaped); only bulk-execute methods still take IQueryable (EF-forced)
type: project
---

**Scope reduced at commit `0ae844f`.**

DRF `ModelSerializer.update(instance, validated_data)` is queryset-naive. Single-row writes match:

- `Serializer.UpdateAsync(TDestination instance, TOrigin origin, ct)` — `Serializer.cs:507-517`
- `Serializer.PartialUpdateAsync(TDestination instance, PartialJsonObject<TOrigin> data, ct)` — `Serializer.cs:478-497`
- `Serializer.DestroyAsync(TDestination instance, ct)` — `Serializer.cs:575-582`
- `Serializer.CreateAsync(TOrigin data, ct)` — `Serializer.cs:459-467`

Controller loads via `GetObjectAsync(IQueryable, id, ct)` composed over the Filters chain; the serializer mutates.

**Remaining divergence — bulk-execute methods only:**
- `UpdateManyAsync(IQueryable, TOrigin, IList<TPrimaryKey>, ct)` — Serializer.cs:539-556
- `DestroyManyAsync(IQueryable, IList<TPrimaryKey>, ct)` — Serializer.cs:623-631
- `GetManyFromDB(IQueryable, IList<TPrimaryKey>, ct)` — Serializer.cs:672-680

**Why:** EF Core `ExecuteDeleteAsync`/`ExecuteUpdateAsync` operate on `IQueryable`. DRF has no bulk-mutate primitive on the serializer; closest analogue is `@action(detail=False)` on a ViewSet calling `QuerySet.update()/.delete()`. EF-forced, not DRF-justified.

**How to apply:** When reviewing the single-row write surface, expect instance-taking signatures and flag any regression to IQueryable. Bulk-execute methods are the exception — keep `IQueryable` and document the bypass list (per-row validators, Perform*Async, change-tracker interceptors).
