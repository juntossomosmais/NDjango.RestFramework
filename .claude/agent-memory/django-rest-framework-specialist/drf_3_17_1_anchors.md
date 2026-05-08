---
name: DRF 3.17.1 pinned anchors
description: Exact file:line citations in DRF 3.17.1 for save/create/update/run_validation/mixins/ListSerializer behaviors. Skip the search next time.
type: reference
---

All paths under `encode/django-rest-framework@3.17.1`.

## `rest_framework/serializers.py`

- `Serializer.update(instance, validated_data)` raises `NotImplementedError` — line 171.
- `Serializer.create(validated_data)` raises `NotImplementedError` — line 174.
- `Serializer.save(**kwargs)` — lines 177-208. Asserts `is_valid` was called and dispatches to `self.update` (when `self.instance` is not None) or `self.create`. The base class itself never writes to a DB.
- `Serializer.run_validation(data)` — lines 444-462 (verified 2026-05-11 against tag 3.17.1; an older note said 442-460 — wrong). Order: `validate_empty_values` -> `to_internal_value` -> `run_validators` -> `validate(value)`. The `assert value is not None, '.validate() should return the validated data'` is on line 458 and enforces that `validate()` returns the validated dict.
- `Serializer.to_internal_value(data)` — lines 493-528. The per-field `validate_<field_name>` lookup is line 510 (`validate_method = getattr(self, 'validate_' + field.field_name, None)`); the return-value semantics are line 514 (`validated_value = validate_method(validated_value)`).
- `ListSerializer.update` — `def update` is at line 732; the multi-line `raise NotImplementedError("Serializers with many=True do not support multiple update by default, only multiple create. For updates it is unclear how to deal with insertions and deletions.")` argument list spans 733-738. Cite as 732-738 (or 731-740 if you want to include surrounding blanks). This is the canonical DRF position on bulk update.
- `ListSerializer.create` — lines 740-743. `return [self.child.create(attrs) for attrs in validated_data]`. Bulk create IS supported by default.
- `ListSerializer.save` — lines 745-771. Same dispatch shape as `Serializer.save`.
- `ModelSerializer.create(validated_data)` — lines 970-1027. Body: `instance = ModelClass._default_manager.create(**validated_data)` at line 1002 plus M2M handling. Yes, the serializer subclass does the actual ORM write.
- `ModelSerializer.update(instance, validated_data)` — `def update` is at line 1040 (verified 2026-05-11; an older note said 1029-1043 — wrong). Method body spans 1040-1056 roughly: `setattr(instance, attr, value)` per field, then `instance.save()` mid-method, then M2M `field.set(value)`.

## `rest_framework/fields.py`

- `Field.run_validation(data)` — lines 521-535. Order: `validate_empty_values` -> `to_internal_value` -> `run_validators`. Field-level validators are NOT short-circuited by serializer-level `validate_<field>`; they run inside `field.run_validation(primitive_value)` BEFORE `validate_<field>` is invoked at the serializer layer.

## `rest_framework/mixins.py`

Definitive counts (verified 2026-05-11 by reading the full file with explicit line ranges):
- `CreateModelMixin` class — line 11. `def create` 15-20. `def perform_create` 22-23 (body `serializer.save()`). `get_success_headers` 25-29.
- `ListModelMixin` class — line 31. `def list` 35-43.
- `RetrieveModelMixin` class — line 46. `def retrieve` 50-53.
- `UpdateModelMixin` class — line 55. `def update` 59-70 (gets the instance via `self.get_object()` at line 61, then `self.perform_update(serializer)` at line 64). `def perform_update` 72-73 (NOT 71-72). `def partial_update` 75-77 (NOT 76-78; sets `kwargs['partial'] = True` and delegates).
- `DestroyModelMixin` class — line 79. `def destroy` 83-86 (loads via `get_object` at 84, calls `self.perform_destroy(instance)` at 85). `def perform_destroy` 88-89.

## `rest_framework/viewsets.py`

- `ModelViewSet` — lines 241-251. Mixin chain: `CreateModelMixin, RetrieveModelMixin, UpdateModelMixin, DestroyModelMixin, ListModelMixin, GenericViewSet`. ModelViewSet itself has no `bulk_*` methods. There is no `BulkModelViewSet` in core.
