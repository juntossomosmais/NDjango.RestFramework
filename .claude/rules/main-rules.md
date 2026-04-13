# Architecture Decision Record

- **Always create tests** for every implementation.
- **Building the solution**: By default, use the filter script to get condensed output:
    ```shell
    docker compose run --volume "$(PWD):/app" --rm --remove-orphans integration-tests bash -c 'dotnet build NDjango.RestFramework.sln > /tmp/build-output.txt 2>&1; cat /tmp/build-output.txt | dotnet dotnet-script ./scripts/filter-build-output.csx'
    ```
  When you need the raw unfiltered output (e.g., to debug a build issue or inspect restore details), run without the script:
    ```shell
    docker compose run --volume "$(PWD):/app" --rm --remove-orphans integration-tests bash -c 'dotnet build NDjango.RestFramework.sln'
    ```
- Run selective unit testing focusing on the classes you have changed. Always on classes, not on methods:
    ```shell
    docker compose run --volume "$(PWD):/app" --rm --remove-orphans integration-tests bash -c 'dotnet test NDjango.RestFramework.sln --settings "./runsettings.xml" --filter "TheClassYouWantToTest" > /tmp/test-output.txt 2>&1; cat /tmp/test-output.txt | dotnet dotnet-script ./scripts/filter-failed-tests.csx'
    ```
- **Coverage for selective testing**: Sample commands to check coverage for a specific file:
    ```bash
    docker compose run --volume "$(PWD):/app" --rm --remove-orphans integration-tests bash -c 'dotnet test NDjango.RestFramework.sln --settings "./runsettings.xml" --filter "TheClassYouWantToTest" > /tmp/test-output.txt 2>&1; cat /tmp/test-output.txt | dotnet dotnet-script ./scripts/generate-coverage-report.csx -- "SampleClass"'
    ```
- Run all tests when the selective runs are successful to ensure overall integrity (you MUST execute exactly like this):
    ```shell
    docker compose run --volume "$(PWD):/app" --rm --remove-orphans integration-tests bash -c 'dotnet test NDjango.RestFramework.sln --settings "./runsettings.xml" > /tmp/test-output.txt 2>&1; cat /tmp/test-output.txt | dotnet dotnet-script ./scripts/filter-failed-tests.csx'
    ```
- When the implementation is fully completed, you can format the code with:
    ```shell
    docker compose run --volume "$(PWD):/app" --rm --remove-orphans lint-formatter dotnet format
    ```

**Important:** Do not pipe `dotnet test` directly into `dotnet dotnet-script` inside the container. Concurrent `dotnet` processes cause coverlet to produce empty coverage data. Always save output to a temp file first, then pipe.

## Coding conventions

- Use "Async" suffix in names of methods that return an awaitable type

## Forbidden patterns

- **Prohibition of the Repository Pattern** — Do not implement the Repository pattern in Entity Framework projects. To reduce complexity and prevent antipatterns, use DbContext directly across the codebase to simplify data access and improve maintainability.
- **No CQRS** — do not implement Command Query Responsibility Segregation.
- **No Mediator** — do not use MediatR or similar libraries. Use direct service injection.
- Legacy `CommandHandlers/`, `QueryHandlers/`, `Commands/`, `Queries/` directories exist but must not be extended.
- AutoMapper is not allowed to be used. In case you touch an existing code, migrate it.
- When validating a collection of items against the database or fetching related data for a list of entities, do not use any pattern that calls a DB method inside a loop — with or without `Task.WhenAll`.

## Data Access

- **Inject `SqlContext` directly** into controllers and service classes. Do not go through a repository layer for new code.
- **Never create new repository classes or interfaces.**
- **Always add `.AsNoTracking()`** on read-only queries. Omitting it is a performance bug — EF Core will track every loaded entity unnecessarily.
- **Add `.AsSplitQuery()`** when a query loads multiple collection `.Include()` chains to avoid the Cartesian explosion problem.
- **Use `ExecuteDeleteAsync()` / `ExecuteUpdateAsync()`** for bulk operations without loading entities into memory first.
- When validating or fetching data for a collection of items, always use a single batched query instead of calling the database once per item.
