---
name: Divergence: validate_<field> return-value semantics
description: DRF's validate_<field>(value) returns the transformed value, which is then assigned to validated_data. Our ValidateAsync supports both return AND mutate.
type: project
---

DRF semantics (serializers.py:493-528, 3.17.1):
- For each writable field, DRF calls `validated_value = field.run_validation(primitive_value)` to get the typed value, then `validated_value = validate_method(validated_value)` if `validate_<field_name>` exists on the serializer, then `self.set_value(ret, field.source_attrs, validated_value)`.
- The **returned value** is what lands in `validated_data` — mutation of `value` in `validate_<field>` would have no effect on the dict because `validated_value` is a primitive in most cases.
- DRF's `Serializer.run_validation` (lines 442-460) similarly does `value = self.validate(value)` and asserts `value is not None`. The cross-field `validate(value)` is also return-value-driven.
- This is enforced by an `assert value is not None, '.validate() should return the validated data'` at line 455.

Our semantics (`Serializer.cs:RunValidationAsync`, lines 304-375):
- Per-field hook: invokes `Validate{Property}Async(currentValue, ...)`, awaits, reads `Result` via reflection, and **writes the result back** onto the DTO if `!Equals(currentValue, newValue)` (lines 324-349). For PATCH, it routes through `PartialJsonObject.SetValue<R>`. So return-value-driven write-back is fully implemented.
- Cross-field hook: `data = await ValidateAsync(...)`. The returned DTO replaces the local `data`. Then for PATCH it syncs every IsSet property back into the partial JSON.
- The DTO is a reference type, so a consumer who mutates `data.SomeProp = newValue` inside `ValidateAsync` AND returns the same `data` reference will see the mutation reflected. A consumer who allocates a new TOrigin and returns it will also work, because we assign `data = await ValidateAsync(...)`.
- A consumer who mutates a property but returns nothing (only possible if they violated the `Task<TOrigin>` return contract) wouldn't compile.

**Why:** Both patterns work in our impl. DRF's docs talk about return-value semantics because Python dicts (validated_data) need explicit assignment; our DTO is a class, so mutation of the same reference is visible to the caller "for free".

**How to apply:**
- Mutation during validation is **acceptable** in our implementation, but tell consumers to prefer return-value semantics for parity with DRF and for clarity (mutating-a-shared-DTO-during-validation invites surprises in cross-field hooks where the order of property reads matters).
- The line `if (!Equals(currentValue, newValue))` is the bridge: it makes per-field hooks behave DRF-correctly even when consumers wrote mutate-style code (the DTO reflects the change either way).
- For PATCH, the SetValue write-back is load-bearing — without it, `IsSet` would still report the original value and `PartialUpdateAsync` would persist the un-normalized data. Do not remove that branch.
