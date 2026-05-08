---
paths: "src/NDjango.RestFramework/Serializer/Serializer.cs"
---

# Serializer

C# port of DRF's `ModelSerializer` (`encode/django-rest-framework@3.17.1`). The serializer owns persistence; it is queryset-naive on single-row writes.

## Single-row writes

- Pass `TDestination instance` as the first parameter of `UpdateAsync`, `PartialUpdateAsync`, `DestroyAsync`. Mirrors `serializers.py:1040+`.
- Do not add any parameter that scopes, filters, or re-loads the row — no `IQueryable<>`, no `Expression<Func<>>`, no queryable composer delegate. The view loads; the serializer mutates.
- Do not add a `TPrimaryKey id` or parameterless overload of any single-row write method.
- Do not add `IQueryable<>` or any scoping parameter to `CreateAsync(data, ct)`. DRF's `ModelSerializer.create(validated_data)` is queryset-free too.

## Bulk-execute methods

- Pass `IQueryable<TDestination> query` as the first parameter of `UpdateManyAsync`, `DestroyManyAsync`, `GetManyFromDB`. Forced by EF Core `ExecuteDeleteAsync`/`ExecuteUpdateAsync`; DRF has no analogue.
- Use `IList<TPrimaryKey>` for id lists. Not `IEnumerable<>`, not `params[]`.
- Implement `DestroyManyAsync` with `ExecuteDeleteAsync`. Do not load entities into the change tracker.
- Keep `UpdateManyAsync` headless-only — no HTTP wiring. DRF refuses bulk-update at `serializers.py:734`.
- Document on every bulk method that it bypasses per-row validators, `Perform*Async`, and tracking-based interceptors.

## Lookup

- Use `GetObjectAsync(IQueryable<TDestination>, TPrimaryKey, CT)` as the only filter-scoped lookup. Mirrors `generics.py:79-102`.
- Do not add an unscoped lookup helper (`GetFromDB(id)`, `GetObjectAsync(id)`). Callers compose their own queryable.
- Do not inject `AsNoTracking()` inside `GetObjectAsync`. Tracking is the caller's decision.

## Validation pipeline

- Run per-field `Validate{Property}Async` hooks before the cross-field `ValidateAsync`. Mirrors `run_validation` at `serializers.py:446-462`.
- Short-circuit to cross-field validation only when per-field hooks added zero errors. Within a tier, collect every error — do not bail on the first.
- Prefer writing back the validator return value. In-place mutation works for reference types but is the secondary path. `serializers.py:460` asserts return-value as canonical.
- Use `ValidationContext<TPrimaryKey>` as the sole carrier into validators. Do not thread `HttpContext`, `IServiceProvider`, or `AsyncLocal` slots through the validation surface.

## XML docs

- Cite DRF analogues as `mirrors DRF's <c>X.y</c> at <c>file.py:L-L</c> of <c>encode/django-rest-framework@3.17.1</c>`.
- Document divergences explicitly. Queryset-taking bulk surface is EF-forced, not DRF-justified.

## Generics

- Do not add a fifth type parameter to `Serializer`. The chain `<TOrigin, TDestination, TPrimaryKey, TContext>` is the porting contract.