---
name: Divergence: bulk operations
description: DRF core has no bulk endpoints; ListSerializer.update raises NotImplementedError by design. We ship BulkDelete (DeleteMany) only.
type: project
---

DRF core ships **no bulk endpoints**. `ModelViewSet` (mixins.py + viewsets.py at 3.17.1) has only single-resource CRUD. Bulk semantics live in third-party `djangorestframework-bulk` (unmaintained) and `drf-extensions`.

`ListSerializer` (serializers.py:731-738) explicitly raises `NotImplementedError` on `.update()` with the message: *"Serializers with many=True do not support multiple update by default, only multiple create. For updates it is unclear how to deal with insertions and deletions."*

`ListSerializer.create` IS supported (line 740-743): `[self.child.create(attrs) for attrs in validated_data]` — equivalent to looped POST.

**Why:** DRF authors view bulk update as semantically ambiguous (do absent items mean delete, ignore, or 404?). They left it as an explicit consumer decision instead of baking a wrong default into core.

**How to apply:**
- The `PutMany` HTTP action was removed (commit e03640f, May 2026). `Serializer.UpdateManyAsync` is preserved as a headless-only primitive (background jobs, admin scripts) — no controller action calls it.
- `DeleteMany` is the only bulk HTTP endpoint and is opt-in via `ActionOptions.AllowBulkDelete = false` by default. Bulk path runs a single `ExecuteDeleteAsync` and bypasses `ValidateDestroyAsync`, the per-row `DestroyAsync` override, EF `SaveChanges` interceptors, and audit-on-delete hooks. Both DRF bulk extensions accept the same bypass — `drf-bulk` calls `qs.delete()` after `filter_queryset`, `drf-extensions` calls `queryset.delete()` after a `pre_delete_bulk` hook.
- DRF + both bulk extensions return `204 No Content` with empty body for both single-resource destroy AND bulk destroy. NEVER echo matched ids on bulk DELETE.
- `BulkUpdate` `SerializerOperation` enum value still exists for the cross-field `ValidateAsync` path; it's used when consumers call `UpdateManyAsync` directly.
- If a consumer needs "list of writes, each with its own body", suggest looping POST or a dedicated endpoint shaped like ListSerializer.create — not a generic mixin in our base controller.
