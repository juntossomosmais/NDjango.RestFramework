---
paths: "src/NDjango.RestFramework/Base/ActionOptions.cs"
---

# ActionOptions

Per-controller toggles that gate HTTP actions on `BaseController`. Library-specific idiom — DRF has no equivalent (DRF disables actions via mixin composition / `http_method_names`); the divergence is intentional and documented in `README.md`.

## Naming

- Name each flag `Allow{ActionName}` matching the controller action exactly: `AllowPatch` ↔ `Patch`, `AllowPut` ↔ `Put`, `AllowDelete` ↔ `Delete`, `AllowBulkDelete` ↔ `DeleteMany`.
- One flag per controller action. Do not group multiple verbs behind a single flag.

## Defaults

- Default `true` (opt-out) when the action's persistence path runs through the documented hook seams (`ValidateDestroyAsync`, `Perform*Async`, EF interceptors, audit, soft-delete). `AllowPatch`, `AllowPut`, `AllowDelete` follow this rule.
- Default `false` (opt-in) when the action silently *bypasses* those seams. `AllowBulkDelete` is the canonical opt-in: `DestroyManyAsync` runs a single `ExecuteDeleteAsync` and skips per-row validation, interceptors, and hooks.
- Document the bypass list in xmldoc whenever a flag defaults `false`. The flag is a discoverable warning, not just a route toggle.

## Wire-up convention

- Enforce each flag as the **first statement** of its action in `BaseController`:
  ```csharp
  if (!_actionOptions.AllowX) return StatusCode(StatusCodes.Status405MethodNotAllowed);
  ```
- Return `405 Method Not Allowed` inline. Do not migrate to `[NonAction]` — `[NonAction]` removes the action from the MVC route table and OpenAPI surface (consumer-invisible) and resolves disabled hits to `404`. The inline `405` keeps the endpoint discoverable in OpenAPI / `OPTIONS` as "documented endpoint, off by default."
- When adding, renaming, or removing a flag: update `README.md` "Disabling endpoints" table, the xmldoc on the corresponding `BaseController` action, and the `BaseController.{Action}` tests that assert `405` when the flag is `false`.

## Scope

- Only mutating actions (`Patch`, `Put`, `Delete`, `DeleteMany`) are flag-gated. Read actions (`GetSingle`, `ListPaged`) and `Post` are intentionally not flag-gated — the absence is by design, not oversight.
- Do not add `AllowGetSingle` / `AllowList` / `AllowPost` reflexively for symmetry. Adding them requires an explicit use case beyond "consistency."
- `ActionOptions` is for **HTTP action gating only**. Do not push unrelated controller knobs (pagination strategy, validation modes, serializer wiring) onto this class.

## Forbidden

- Do not replace `Allow*` flags with `[NonAction]` overrides. The mechanisms are not semantically equivalent (route presence, OpenAPI surface, `OPTIONS` discovery, status code on disabled hits all differ).
- Do not return `404` from a disabled action. The contract is `405 Method Not Allowed`.
- Do not use `ActionOptions` as a feature-flag bag. One job: HTTP action gating.
