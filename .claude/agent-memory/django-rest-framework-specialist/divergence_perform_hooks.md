---
name: Divergence: perform_* hooks at view layer
description: BaseController exposes Perform{Create,Update,PartialUpdate}Async (post-93f128b) and ValidateDestroyAsync. Hook signature receives the DTO (not a bound serializer) so kwargs-pass-through to save() is unavailable; side-effect-after-save and transactional-wrap work cleanly. AllowBulkDelete=false default is the opt-in gate for the lifecycle-bypassing ExecuteDeleteAsync bulk path.
type: project
---

DRF view-layer hooks (mixins.py at 3.17.1):
- `perform_create(serializer)` lines 22-23 — default `serializer.save()`.
- `perform_update(serializer)` lines 73-74 — covers PUT and PATCH (PATCH re-enters via `partial_update` lines 77-79 setting `kwargs['partial']=True`).
- `perform_destroy(instance)` lines 94-95 — default `instance.delete()`.

DRF passes the bound serializer so consumers can do `serializer.save(owner=request.user)` — the kwargs flow into `Serializer.save()` and become part of `validated_data` before the single ORM commit.

Our shape (`BaseController.cs`, post-2026-05 refactor):
- `Post` line 144: calls `PerformCreateAsync(entity, ct)` (hook at lines 171-174, default delegates to `_serializer.CreateAsync`).
- `Put` line 260: calls `PerformUpdateAsync(id, origin, ct)` (hook at lines 290-294, default delegates to `_serializer.UpdateAsync`).
- `Patch` line 200: calls `PerformPartialUpdateAsync(id, entity, ct)` (hook at lines 232-236, default delegates to `_serializer.PartialUpdateAsync`).
- `Delete` line 316: calls `ValidateDestroyAsync(instance, errors, ct)` (hook at lines 350-353) BEFORE `_serializer.DestroyAsync(instance, ct)` line 320. Hook is **validate-only** — explicit XML doc lines 324-349. Actual delete-site override is `Serializer.DestroyAsync(TDestination, ct)` at `Serializer.cs:554-561`.
- `DeleteMany` line 361: inlines `_serializer.DestroyManyAsync` (no hook by design — the bulk path is non-DRF surface area).
- `PutMany`: removed entirely. `Serializer.UpdateManyAsync` survives as headless-only.

**Signature divergence and its cost:**
- DRF receives the serializer; we receive the DTO. Consumer cannot pass kwargs through to `Serializer.save()` the way DRF allows. Workarounds: (1) mutate the DTO before calling `base.PerformCreateAsync` — works only if the field is on the DTO; (2) push the logic into `Serializer.CreateAsync`/`MapToDestination` with `IHttpContextAccessor` — README:361 recommends this when logic is shared with non-HTTP callers.
- Stamping a request-scoped value (e.g., `User.Id`) *before* the first INSERT is awkward through the controller hook alone. The `PerformHookCustomersController` test (`Support/Controllers.cs:439-474`) demonstrates the post-save-and-save-again pattern, which is fine for auditing but costs an extra round-trip.
- For transactional wrapping, side-effect-after-save, and outbox dispatch the current hooks are sufficient.

**`ValidateDestroyAsync` is intentionally NOT DRF's `perform_destroy`:**
- Naming was renamed from `PerformDestroyAsync` to remove the false-friend with DRF. The hook is a state-predicate seam ("address is main and others exist") that short-circuits to 400 via the errors dict.
- The actual delete-site override is `Serializer.DestroyAsync(TDestination, ct)` — that's where transactions, outbox, authoritative re-checks belong.

**How to apply:**
- "Where do I stamp `request.user` on POST?" → recommend `Serializer.CreateAsync` override with `IHttpContextAccessor`. The controller hook works for after-save audit-log writes but not for stamping fields before the first INSERT without two round-trips.
- "How do I wrap PUT in a transaction?" → override `PerformUpdateAsync` on the controller, wrap `base.PerformUpdateAsync(...)` in `BeginTransactionAsync`. Same for create and patch.
- "What's the analogue of DRF's `perform_destroy`?" → `Serializer.DestroyAsync(TDestination, ct)`, NOT `ValidateDestroyAsync`. The latter is for pre-delete state validation only.
