---
name: ClientErrorResultFilter does not rewrite BadRequestObjectResult on net8.0
description: BadRequestObjectResult is not IClientErrorActionResult, so a custom envelope inside BadRequest(value) is preserved on [ApiController] controllers
type: reference
---

On net8.0 (`release/8.0`), `[ApiController]` registers `ClientErrorResultFilter` (gated by `ApiBehaviorOptions.SuppressMapClientErrors=false`, default), and that filter is what would normally swap a 4xx result for a stock `ProblemDetails`. The filter only fires when `context.Result is IClientErrorActionResult` — see `src/Mvc/Mvc.Core/src/Infrastructure/ClientErrorResultFilter.cs:OnResultExecuting` (release/8.0).

Critical detail for this library: `BadRequestObjectResult` (the result returned by `ControllerBase.BadRequest(object)`) inherits `ObjectResult`, which is `IStatusCodeActionResult` — NOT `IClientErrorActionResult`. The interface is defined empty at `src/Mvc/Mvc.Core/src/Infrastructure/IClientErrorActionResult.cs` and is implemented by `BadRequestResult` (no body) via `StatusCodeResult`, not by `BadRequestObjectResult`.

Practical implication for NDjango.RestFramework: `BadRequest(new ValidationErrors(...))` on a `[ApiController]`-decorated subclass passes through unchanged — the envelope ships to the client verbatim. No need to suppress `MapClientErrors` to keep the custom shape.

Caveat to re-verify on TFM bump: if 9.0/10.0 ever change `BadRequestObjectResult` to implement `IClientErrorActionResult` (it would be a breaking change for anyone returning `BadRequest(custom)`, but worth checking), the rewrite would suddenly start replacing our envelope with stock ProblemDetails on every validation failure.
