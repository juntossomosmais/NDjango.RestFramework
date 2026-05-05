---
name: Project TFM (as of 2026-05)
description: NDjango.RestFramework targets net8.0; aspnetcore source lookups must use release/8.0
type: project
---

`src/NDjango.RestFramework/NDjango.RestFramework.csproj` declares `<TargetFramework>net8.0</TargetFramework>`. Test project `tests/NDjango.RestFramework.Test/NDjango.RestFramework.Test.csproj` is also net8.0.

**Why:** Pin every `dotnet/aspnetcore` and `dotnet/runtime` source lookup to `release/8.0`. Behavior of hosted service start ordering, ProblemDetails defaults, and `[ApiController]` 400 short-circuit are all version-sensitive.

**How to apply:** When citing source, use `release/8.0` branch. Re-detect TFM at the start of each session — if the consumer upgrades to net9/net10, re-verify all anchors.
