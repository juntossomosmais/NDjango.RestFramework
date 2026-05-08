---
name: Bulk action stance vs DRF
description: HTTP PutMany was dropped, UpdateManyAsync kept as headless primitive, DestroyManyAsync rewritten on ExecuteDeleteAsync
type: project
---

DRF 3.17.1 has **no bulk endpoints in core**. `mixins.py` has only the 5 single-instance mixins; `viewsets.py` exposes only `ReadOnlyModelViewSet` and `ModelViewSet`. Bulk lives in third-party libs like `djangorestframework-bulk` and is layered on `ListSerializer` (`serializers.py:135` — `many_init`).

**Current state:**

- HTTP `PutMany` action — **removed** from `BaseController`. README has an explicit "no HTTP bulk-update verb" callout citing DRF's `ListSerializer.update` `NotImplementedError` rationale.
- `Serializer.UpdateManyAsync(origin, ids, ct)` — **kept** as a public headless primitive. Used by `MappingSeamSpySerializer` (proves `ApplyToDestination` is reached per-entity), `ValidatingCustomerSerializer` (proves the `BulkUpdate` validation context is honored end-to-end), and listed in README as "Headless-only — there is no HTTP action that calls this."
- HTTP `DeleteMany` action — **kept**. The serializer's `DestroyManyAsync` was rewritten to use `ExecuteDeleteAsync` (no entity load, no change-tracker). The implementation honors the project rule in CLAUDE.md mandating `ExecuteDeleteAsync` for bulk operations.

**Pending architectural simplification (2026-05-09):** the existing `AsNoTracking` projection that produces `existingIds` for the response body is a half-truth — there is a documented TOCTOU between the projection and the `ExecuteDeleteAsync` statement, so the returned list is "matched at projection time," not "actually deleted." Recommendation is to drop the projection, change `DestroyManyAsync` to return `Task`, and have the controller action return `204 No Content` (symmetric with single-resource `Delete`, matches DRF's `DestroyModelMixin`). Pre-release license covers the breaking change.

`DestroyManyAsync` is `public virtual` — consumers needing soft-delete or audit-log can override it on a custom serializer. The set-based default and the override-and-replace seam coexist.

`ListSerializer<T>` analogue: defer indefinitely. Only relevant if we commit to "POST list-of-DTOs to bulk-create," which is itself non-DRF surface.

**Why:** Pre-release license to remove non-DRF endpoints is wide. `PutMany`'s validation pipeline was structurally unable to express the operation correctly (single context for many entities). `DestroyManyAsync`'s pre-rewrite implementation was a known performance bug.

**How to apply:** If asked about adding a bulk-create or bulk-update HTTP verb, push back — those are non-DRF and were intentionally not added. If asked about `UpdateManyAsync`'s public surface looking dead, point to its tests in `MappingSeamTests.cs` and `SerializerValidateAsyncTests.cs` — it has a real headless contract worth keeping. If asked about extending `DestroyManyAsync` for soft-delete/audit, point to the `virtual` override.
