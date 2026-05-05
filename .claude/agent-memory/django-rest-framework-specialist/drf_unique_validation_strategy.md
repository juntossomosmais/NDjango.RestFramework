---
name: DRF unique-validation strategy
description: DRF's stance on uniqueness is to validate at the serializer (UniqueValidator), not translate DB exceptions
type: reference
---

DRF 3.17.1 `rest_framework/validators.py:38-83` — `UniqueValidator`:

```python
class UniqueValidator:
    requires_context = True
    def __call__(self, value, serializer_field):
        ...
        queryset = self.filter_queryset(value, queryset, field_name)
        queryset = self.exclude_current_instance(queryset, instance)
        if qs_exists(queryset):
            raise ValidationError(self.message, code='unique')
```

Auto-injected by `ModelSerializer` for any model field with `unique=True`. There is no DRF code that catches Django's `IntegrityError` and re-raises a 400. If a race slips past the validator and the DB unique index fires, it surfaces as a 500.

The "right" pattern in DRF:
1. Pre-write validator to catch the common case → 400 with field-keyed error.
2. DB unique constraint as the backstop for races → 500 in vanilla DRF; consumers add custom exception handlers if they care.

For NDjango.RestFramework, EF Core gives us `DbUpdateException` + provider-specific `SqlException`/`PostgresException`. The library can offer real value here that DRF doesn't:
1. A `Unique.CheckAsync` helper for the validator path (DRF parity).
2. A `protected virtual Exception? TranslateDbException(DbUpdateException)` hook on Serializer so consumers don't write try/catch around every save (this is BETTER than DRF, not just parity).

Don't try to auto-detect index names — that's per-app SQL-namespacing knowledge. Provide the hook, let the consumer map index name → field/message.
