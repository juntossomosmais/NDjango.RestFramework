---
name: DRF partial validation evidence
description: Pinned DRF 3.17.1 source anchors proving that on PATCH validate(attrs) sees only fields the client sent
type: reference
---

DRF 3.17.1, key line ranges:

- `rest_framework/fields.py:457-491` — `Field.validate_empty_values`. Branch `if data is empty: if getattr(self.root, 'partial', False): raise SkipField()`. This is the mechanism that drops absent fields on partial.
- `rest_framework/serializers.py:481-527` — `Serializer.to_internal_value`. Loops `_writable_fields`, calls `field.run_validation(primitive_value)`, catches `SkipField` with `pass` (no entry in `ret` dict). The dict that's returned is what `validate(attrs)` receives.
- `rest_framework/serializers.py:441-453` — `Serializer.run_validation`. Order is: `to_internal_value` (which dropped absent fields), then `run_validators`, then `self.validate(value)`. So `validate()` receives only present fields on partial.

Practical implication: the Python idiom `if 'is_main' in attrs and attrs['is_main'] is False:` is the canonical way to write "client explicitly sent IsMain=false on PATCH". This is what consumers in our C# codebase want to express but currently can't from inside the materialized-DTO-based `ValidateAsync(TOrigin, ValidationContext, errors)` overload.
