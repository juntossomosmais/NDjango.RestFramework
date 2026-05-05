---
name: Consumer pain points spotted 2026-05
description: Concrete divergences from DRF observed in dotnet-template that drive recommended library changes; line-counted impact
type: project
---

Audit date: 2026-05-05. Consumer: `/Users/ext.williansla/Development/git/dotnet-template`.

**Why:** Library has not launched yet, so breaking changes are still on the table. The consumer is a real production-shaped app (transactions, CAP outbox, FK locks, race-translation). Friction observed there is signal for what to fix before v1.

**How to apply:** When advising on library design changes, weight these by the line-count savings in `AddressesController.cs` (currently 499 lines):

- **#3 dual ValidateAsync overload on PATCH** — biggest pain. Consumer wrote `validate(attrs)` rule twice across `ValidateAsync(AddressDto, ValidationContext, errors)` and `ValidateAsync(PartialJsonObject<AddressDto>, id, errors)`. Caused by ValidationContext not exposing a presence probe. Fix: add `IPresenceProbe` to ValidationContext, deprecate the second overload. ~38 lines saved.
- **#5 single-hook CreateAsync/UpdateAsync mixes 8 concerns** — transactions, locks, business rules, entity construction, save, outbox, commit, exception translation all in one method. Fix: split BuildEntity (pure mapping) from PerformCreateAsync/PerformUpdateAsync/PerformDestroyAsync (I/O wrapper). Mirrors DRF perform_create. ~10 lines saved + far better review surface.
- **#10 try/catch around every save for unique-index races** — three identical try/catch blocks per serializer. Fix: `protected virtual Exception? TranslateDbException(DbUpdateException)` hook on Serializer, called once by PerformXAsync. ~15 lines saved per serializer.
- **#2 IsSet ladder on PartialUpdateAsync** — consumer manually walks every property. Fix: `protected virtual void ApplyPartialToEntity(...)` default impl. ~20 lines saved.
- **#4 null! sentinel for 404** — sprinkled across all 3 mutating methods. Fix: framework-owned GetObjectAsync, hooks receive resolved entity. ~12 lines saved + fixes a race window the consumer is fighting.
- **#1 no toggle for Delete/DeleteMany/PutMany** — SalesController has 3 overrides returning 405 manually. Fix: extend ActionOptions to all 8 verbs.

NOT to fix:
- **#9 nested writes** — DRF doesn't ship this; consumer's manual loop is correct. No-op.
- **#8 hook return-value semantics** — already match DRF; doc-only fix.

Cumulative `AddressesController.cs` savings if all of #1-#7+#10 ship: ~110 lines (499 → ~380).

NOTE: this file is a snapshot. Re-verify against the consumer codebase before acting — it could have evolved.
