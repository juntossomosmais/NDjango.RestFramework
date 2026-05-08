---
name: Mapping NDjango Serializer to DRF ModelSerializer
description: Our Serializer class corresponds to DRF's ModelSerializer (the ORM-aware subclass), not the abstract base Serializer.
type: project
---

Our `Serializer<TOrigin, TDestination, TPrimaryKey, TContext>` at `src/NDjango.RestFramework/Serializer/Serializer.cs` is **the C# analogue of DRF's `ModelSerializer`**, not DRF's base `Serializer`. The class:
- Owns a `DbContext` (`_dbContext`).
- Implements `CreateAsync` / `UpdateAsync` / `PartialUpdateAsync` / `UpdateManyAsync` / `DestroyAsync` / `DestroyManyAsync` as concrete ORM-writing methods (each calls `_dbContext.SaveChangesAsync`).

DRF parity:
- DRF `ModelSerializer.create` -> `ModelClass._default_manager.create(**validated_data)`. Ours -> `_dbContext.Set<TDestination>().AddAsync(...)` + `SaveChangesAsync`.
- DRF `ModelSerializer.update` -> `setattr` loop + `instance.save()`. Ours -> `ApplyToDestination` + `_dbContext.Update` + `SaveChangesAsync`.

**Why:** DRF base `Serializer.create/update` are abstract (raise `NotImplementedError`); only `ModelSerializer` ships ORM behavior. Our type is concrete and ships ORM behavior, so the parity claim is to `ModelSerializer`, not the abstract base.

**How to apply:** When discussing semantics, treat our `Serializer` as `ModelSerializer`. We do NOT have a separate abstract `Serializer` class; if a future feature needs the abstract serializer surface (read-only DTO transforms, custom `create` shapes), add it as a new type — do not retrofit the existing class. The DRF philosophy is that `Serializer` is the abstract base and `ModelSerializer` is one (canonical) ORM-writing concrete subclass; users can write their own concrete subclasses with custom create/update.
