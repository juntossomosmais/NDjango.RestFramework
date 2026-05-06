---
name: DRF puts side effects outside the serializer
description: DRF has no on_before/after_save hooks and docs explicitly steer side effects to caller-side overrides or Django signals
type: reference
---

DRF's save lifecycle is `is_valid()` → `save(**kwargs)` → `create`/`update` (`rest_framework/serializers.py:175-216`). There are no pre/post-save hooks anywhere in `serializers.py`, `generics.py`, `mixins.py`, or `viewsets.py`. The `docs/api-guide/serializers.md` "Overriding `.save()` directly" section advises overriding `save()` itself when the create/update names are not meaningful (e.g. send_email contact form). Post-persist side effects in Django are conventionally handled by `post_save` signals, which sit outside the serializer entirely.

How to apply: when someone proposes `OnBeforeSaveAsync` / `OnAfterSaveAsync` lifecycle hooks plus a "suppress side effects" flag for the C# port, push back. That's importing a non-DRF idea. The suppress flag is especially smelly — it forces the serializer to know about its own side effects so it can opt out, which is precisely what signals avoid. The DRF-parity answer is: keep `CreateAsync`/`UpdateAsync` as the override seam, and have callers compose with a separate publisher service (or scope transactions at the call site for outbox cases).
