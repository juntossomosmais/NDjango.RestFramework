---
name: Hosted service startup order (net8.0)
description: Where Host.StartAsync iterates hosted services and where Kestrel actually binds
type: reference
---

**Source pins (release/8.0):**

- `dotnet/runtime`: `src/libraries/Microsoft.Extensions.Hosting/src/Internal/Host.cs` — `Host.StartAsync` resolves `IEnumerable<IHostedService>` from DI, then calls `ForeachService(_hostedServices, ..., abortOnFirstException: !concurrent, ...)`. The default is sequential (`ServicesStartConcurrently == false`), so any `StartAsync` exception is captured and re-thrown via `LogAndRethrow()` — host startup fails, no further services run, no listener binds.
- `dotnet/aspnetcore`: `src/Hosting/Hosting/src/GenericHost/GenericWebHostService.cs` — `GenericWebHostService` is itself an `IHostedService`. Its `StartAsync` is what calls `await Server.StartAsync(httpApplication, cancellationToken)` (the Kestrel bind). It is registered through `GenericWebHostBuilder` via `AddHostedService<GenericWebHostService>()` *after* user-registered hosted services from `ConfigureServices`.

**Implications:**
- `services.AddHostedService<ControllerFieldValidationHostedService>()` registered in `ConfigureServices` fires *before* Kestrel binds.
- A throw in our `StartAsync` propagates out of `Host.StartAsync` → host process exits before serving any request. This is genuine fail-fast.
- Multiple hosted services run in registration order. Our validation hosted service must be registered before any consumer hosted service that depends on a healthy controller graph (rare in practice).
