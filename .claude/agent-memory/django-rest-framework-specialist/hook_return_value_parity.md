---
name: Hook return-value parity with DRF
description: Confirmed that NDjango.RestFramework's per-field Validate{X}Async return-value-replaces semantics matches DRF validate_<field>
type: reference
---

DRF 3.17.1 `rest_framework/serializers.py:493-498`:

```python
validate_method = getattr(self, 'validate_' + field.field_name, None)
primitive_value = field.get_value(data)
try:
    validated_value = field.run_validation(primitive_value)
    if validate_method is not None:
        validated_value = validate_method(validated_value)
```

The return of `validate_<field>` REPLACES the value that goes into the validated_data dict.

Our parity:

`src/NDjango.RestFramework/Serializer/Serializer.cs:322-345` — `RunValidationAsync`. Reads `Result` of the hook task, and:
- POST/PUT (no partial): `property.SetValue(data, newValue)` writes back to the DTO.
- PATCH (partial != null): `closedSetValue.Invoke(partialData, ...)` updates the underlying JObject so subsequent `Instance` reads see the normalized value.

So consumer normalizers like `AddressesController.ValidateCepAsync` returning the digits-only CEP string DO take effect downstream. This parity is undocumented in the library — recommend adding it to the XML doc on `RunValidationAsync` / per-field hook discovery section (Serializer.cs:179-185).
