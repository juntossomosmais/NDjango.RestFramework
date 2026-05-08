---
name: Filter-queryset scope on writes — closed at 0ae844fa, kept as historical reference
description: Previously Put/Patch/Delete/DeleteMany bypassed the Filters chain (cross-tenant mutation gap). Closed by commit 0ae844fa (2026-05-08) which routes every row-resolving action through FilterQuery(GetQuerySet()) before GetObjectAsync(query, id). Kept for reference — the DRF behavior and historical rationale still apply when anyone considers a new write action.
type: reference
---

# Status: RESOLVED at HEAD `0ae844fa` (2026-05-08)

Commit `0ae844fa` ("feat: port write surface to DRF ModelViewSet semantics") rewires every row-resolving action (`Patch`, `Put`, `Delete`, `DeleteMany`) to compose the `Filters` chain before resolving the id:

```csharp
var query = FilterQuery(GetQuerySet(), HttpContext.Request);
var instance = await _serializer.GetObjectAsync(query, id, ct);
if (instance is null) return NotFound();
```

`Serializer.GetObjectAsync(IQueryable, TPrimaryKey, ct)` is the sole filter-scoped lookup; the unscoped `GetFromDB` and the prior `Set<T>().FindAsync` paths were removed.

Verified at 0ae844fa:
- `BaseController.Patch` line 228, `Put` line 320, `Delete` line 398, `DeleteMany` line 508 — each calls `FilterQuery(GetQuerySet(), HttpContext.Request)` first.
- `Serializer.UpdateAsync(instance, ...)`, `PartialUpdateAsync(instance, ...)`, `DestroyAsync(instance, ...)` take the loaded instance; no re-fetch.
- `DestroyManyAsync(IQueryable query, IList<TPrimaryKey> ids, ct)` composes the predicate over the filter-scoped query.

# DRF behavior at 3.17.1 (the parity target)

- `rest_framework/generics.py` line 67–89: `GenericAPIView.get_object()` calls `queryset = self.filter_queryset(self.get_queryset())` BEFORE `get_object_or_404(queryset, **filter_kwargs)`. So every action that uses `get_object()` inherits the full filter chain.
- `rest_framework/generics.py` line 123–127: `filter_queryset(queryset)` iterates `self.filter_backends` and applies each.
- `rest_framework/mixins.py`:
  - `RetrieveModelMixin.retrieve` calls `self.get_object()` (line 51).
  - `UpdateModelMixin.update` calls `self.get_object()` (line 61).
  - `UpdateModelMixin.partial_update` delegates to `update` (line 76).
  - `DestroyModelMixin.destroy` calls `self.get_object()` (line 84).

So in DRF: `filter_backends` are applied on read AND single-object write paths. There is no bulk-delete in core DRF, so the question doesn't arise there.

# DRF design intent (kept for context)

`filter_backends` is meant for queryset-scoping in general — it is the seam where SearchFilter, OrderingFilter, DjangoFilterBackend, AND custom row-level scoping (tenant filters, soft-delete filters) all plug in. The DRF docs explicitly recommend overriding `get_queryset()` for per-user scoping — but because `get_object()` ALSO calls `get_queryset()` AND `filter_queryset()`, both seams cover writes too. Object-level permissions (`has_object_permission`) are a separate, complementary mechanism that runs AFTER the row is loaded — we do not have an analogue (gap).

# Why this stays in memory

- Future write actions added to `BaseController` MUST route through `FilterQuery(GetQuerySet())` before resolving the id. The `.claude/rules/base-controller.md` "Filter scoping" section codifies this; this memory carries the historical "why".
- A test (`CrossTenantWriteScoping` in `tests/.../BaseControllerTests.cs`) pins the contract.

# Gaps that remain

- No analogue for DRF's `check_object_permissions(self.request, obj)` (runs after `get_object` resolves). If/when the project gains an object-level permissions hook, this is where it lands.
