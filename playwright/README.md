# Playwright tests

End-to-end CRUD + OpenAPI contract tests for `NDjango.RestFramework`, driven through `sample-project/`.

## Prerequisites

- Node.js 20+
- .NET 8 SDK
- Docker (the SQL Server `db` service from `docker-compose.yml`)

## Local

```bash
# 1. Start SQL Server (port 1433)
docker compose up -d db

# 2. Install playwright + browsers (once)
cd playwright
npm install
npm run install-browsers

# 3. Run tests — webServer block in playwright.config.ts auto-starts sample-project on :8000
npm test
```

`webServer.reuseExistingServer = true` means you can leave `sample-project` running between test runs.

## Layout

```
playwright/
├── helpers/data.ts          # tiny unique() helper to dodge unique-index collisions
├── tests/openapi.spec.ts    # OpenAPI document contract assertions
├── tests/crud.spec.ts       # CRUD per resource (Categories, Restaurants, ...)
├── playwright.config.ts     # chromium project, sequential, baseURL=http://localhost:8000
├── tsconfig.json
└── package.json
```

Sequential by design (`fullyParallel: false`, `workers: 1`) — the sample shares one SQL Server database, and a couple of tests rely on exact counts on the collection endpoints. Relax once we want per-test isolation.

## CI

`.github/workflows/playwright.yml` brings up `db`, builds + starts `sample-project`, then runs `npx playwright test`. The HTML report is uploaded on every run; the sample's stdout log only on failure.
