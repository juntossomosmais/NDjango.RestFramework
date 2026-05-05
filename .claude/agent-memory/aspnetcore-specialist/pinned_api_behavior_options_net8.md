---
name: [ApiController] / InvalidModelStateResponseFactory / ProblemDetails wiring (net8.0)
description: How the ModelState invalid 400 short-circuit reaches the configured factory, and how AddProblemDetails plugs in
type: reference
---

**Source pins (release/8.0):**

- `src/Mvc/Mvc.Core/src/Infrastructure/ModelStateInvalidFilter.cs` — `[ApiController]` adds this filter (Order = -2000). In `OnActionExecuting` it sets `context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context)`. So `ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ...)` is the supported hook for `[ApiController]` controllers.
- `src/Mvc/Mvc.Core/src/DependencyInjection/ApiBehaviorOptionsSetup.cs` — On release/8.0 this is **`IConfigureOptions<ApiBehaviorOptions>`** (NOT `IPostConfigureOptions`). It **unconditionally** assigns `options.InvalidModelStateResponseFactory = context => ...`; it does NOT guard with `if (factory == null)`. Default factory resolves `ProblemDetailsFactory` from DI per request and returns `BadRequestObjectResult(problemDetails)` with `application/problem+json`.
- Registered via `MvcCoreServiceCollectionExtensions.AddMvcCoreServices`: `services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ApiBehaviorOptions>, ApiBehaviorOptionsSetup>())`.
- `OptionsFactory.Create` (dotnet/runtime, `Microsoft.Extensions.Options/src/OptionsFactory.cs`) iterates `_setups` in **registration order**, then `_postConfigures`. For options assignments (last-writer-wins), the order across multiple `IConfigureOptions` is what matters.
- `src/Mvc/Mvc.Core/src/Infrastructure/DefaultProblemDetailsFactory.cs` — Honors `ProblemDetailsOptions.CustomizeProblemDetails` (.NET 7+ hook).
- `src/Http/Http.Extensions/src/ProblemDetailsServiceCollectionExtensions.cs` — `services.AddProblemDetails()` is middleware-level (`IProblemDetailsService`, `IProblemDetailsWriter`), independent of MVC's `InvalidModelStateResponseFactory`.

**Implications for overriding the factory:**

- `services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ...)` works **only when called AFTER `AddControllers()/AddMvcCore()`** because `TryAddEnumerable` appends user setup to the end and `OptionsFactory` runs setups in registration order — last-writer-wins.
- If called **before** `AddControllers()`, the framework's `ApiBehaviorOptionsSetup` runs second and clobbers the user factory. This is a foot-gun.
- `services.PostConfigure<ApiBehaviorOptions>(...)` is **strictly safer** because all post-configures run after all configures, regardless of registration order. Recommend this over plain `Configure` for the response factory.
- Our `ConfigureValidationResponseFormat` overrides the factory — wins over the default `ProblemDetails` factory inside `[ApiController]` only if registered after `AddControllers()`.
