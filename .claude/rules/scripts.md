---
paths: "scripts/**/*.csx"
---

# Scripts

## C# Scripting (.csx)

- All project scripts are C# script files (`.csx`) executed via `dotnet dotnet-script`.
- Do **not** create bash (`.sh`) scripts for tooling. Use `.csx` files instead.
- Scripts use top-level statements — no `Main` method or class wrapper required.
- Pass arguments after `--` separator: `dotnet dotnet-script ./scripts/example.csx -- "arg1"`.
- Access arguments via `Args` (e.g., `Args[0]`).
- Stdin piping is supported: `dotnet test ... | dotnet dotnet-script ./scripts/example.csx`.
