---
name: EF Core 8 ExecuteDelete change-tracker isolation
description: Verified contract that ExecuteDelete/ExecuteDeleteAsync issue SQL directly without involving the change tracker, and are not enrolled in SaveChanges transactions.
type: reference
---

Verified on dotnet/EntityFramework.Docs @ main against EF Core 8.x (project targets `net8.0`):

`entity-framework/core/saving/index.md`:
"This executes very efficiently in the database, without loading any data from the database or involving EF's change tracker."

`entity-framework/core/saving/execute-insert-update-delete.md`:
"Since `ExecuteUpdate` and `ExecuteDelete` do not interact with the change tracker, they cannot automatically apply concurrency control."

Implications used in audits:
- ExecuteDeleteAsync issues its own SQL roundtrip — change-tracker state is unmodified by the call itself.
- It is NOT batched with pending SaveChangesAsync — caller must wrap both in an explicit `BeginTransactionAsync` for atomicity.
- Tracked entries pointing at deleted rows survive in the change tracker as "phantoms"; subsequent SaveChangesAsync on unrelated changes can throw or silently re-insert. Detach with `Entry(e).State = Detached` or `ChangeTracker.Clear()` if the same scope continues.
- No optimistic concurrency token check is applied — implement manually if needed.

Re-verify when project bumps EF Core major.
