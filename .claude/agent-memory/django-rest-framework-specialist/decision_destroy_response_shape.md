---
name: Decision: destroy response shape (single + bulk)
description: DRF and both mainstream bulk extensions return 204 empty body for destroy; bulk DELETE must not echo matched ids.
type: project
---

For both single-resource destroy and bulk destroy, the established DRF idiom is **204 No Content with empty body**. Verified at tag 3.17.1 across core and the two mainstream bulk packages:

- DRF core `DestroyModelMixin.destroy` — `rest_framework/mixins.py:83-91` — `Response(status=status.HTTP_204_NO_CONTENT)`.
- `djangorestframework-bulk` (miki725) `BulkDestroyModelMixin.bulk_destroy` — `rest_framework_bulk/drf3/mixins.py:88-95` — same 204 empty.
- `drf-extensions` (chibisov) `ListDestroyModelMixin.destroy_bulk` — `rest_framework_extensions/bulk_operations/mixins.py:34-42` — same 204 empty (explicitly discards Django's `(count, {label: count})` tuple).

**Why:** DRF treats DELETE as side-effect, not query. Echoing matched ids implies a partial-success contract the implementation cannot honor (TOCTOU between projection and delete). All three surveyed implementations throw away even the affordances they get for free (Django's row count tuple).

**How to apply:**
- `Serializer.DestroyManyAsync` should be a single `ExecuteDeleteAsync()` roundtrip and the action should return `NoContent()`. No projection roundtrip. No "deletedIds" body.
- If a consumer needs "which ids were missing," it is their job to probe with GET/HEAD before DELETE — not a side-channel on the DELETE response.
- Internally, `ExecuteDeleteAsync` returns `int` rows affected — keep it server-side (logging/metrics), do not surface in the response body.
- This applies to any future destroy variants too: filtered destroy, soft-delete, etc. — body should remain empty unless we have an explicit, non-DRF reason to diverge and document it.
