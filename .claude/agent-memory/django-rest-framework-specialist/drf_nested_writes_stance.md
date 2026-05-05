---
name: DRF nested writes stance
description: DRF actively blocks nested writes by default; consumers must override create/update — there is no auto-magic
type: reference
---

DRF 3.17.1 `rest_framework/serializers.py:1019` — first line of `ModelSerializer.update`:

```python
def update(self, instance, validated_data):
    raise_errors_on_nested_writes('update', self, validated_data)
```

`raise_errors_on_nested_writes` raises an `AssertionError` if validated_data contains keys whose values are nested serializer outputs. Same pattern in `ModelSerializer.create`. The DRF philosophy: nested writes have FK ordering, cascade, and partial-vs-full ambiguity that no library can solve generically — make the consumer write it explicitly.

`docs/topics/writable-nested-serializers.md` at 3.17.1 is essentially incomplete (last meaningful update 2019-07-13 per file metadata). Stub headers "Validation errors", "Adding and removing items", "Making PATCH requests" have no body.

`ListSerializer` (`rest_framework/serializers.py:590+`) is for COLLECTION-level inputs (`POST /resource/` with array body), not for nested children inside a parent payload. Don't confuse the two.

Implication for NDjango.RestFramework: do NOT ship auto-handling for nested children in CreateAsync/UpdateAsync. The consumer's manual loop in StoreSerializer.CreateAsync (parent first, then SaveChanges, then children with foreign key set) is the DRF-endorsed pattern. Recommend documenting this — not solving it.
