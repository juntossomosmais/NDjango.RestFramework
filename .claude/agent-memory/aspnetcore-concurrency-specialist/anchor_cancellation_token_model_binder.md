---
name: CancellationTokenModelBinder pinned anchor (net8.0)
description: Source-pinned proof that ASP.NET Core 8 binds a controller action's CancellationToken parameter to HttpContext.RequestAborted, so the project's "when invoked through BaseController, CT is RequestAborted" doc is accurate.
type: reference
---

Verified on dotnet/aspnetcore @ release/8.0:
src/Mvc/Mvc.Core/src/ModelBinding/Binders/CancellationTokenModelBinder.cs

The entire binder is one line of behavior:
`var model = (object)bindingContext.HttpContext.RequestAborted;`
`bindingContext.Result = ModelBindingResult.Success(model);`

Implications used in audits:
- Any `CancellationToken` parameter on a controller action (default value `default`) is replaced at bind time by `HttpContext.RequestAborted`.
- The `default` literal in the action signature is a no-op fallback — the binder always fires for `CancellationToken`-typed parameters.
- "Default" of `CancellationToken.None` only ever surfaces if the action is invoked outside MVC's binder pipeline (i.e., headless callers calling the serializer directly).

Re-verify on framework bumps: this binder has been stable across 6/7/8/9, but the pin must be re-pointed at the new branch when `<TargetFramework>` moves.
