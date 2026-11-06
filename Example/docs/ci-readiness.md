# CI Readiness Guide

This document is the authoritative reference for running the full Minded test suite — locally
or in any CI environment. The goal is deterministic, repeatable runs from a single command.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 20+ | `node --version`; used by Playwright |
| [PowerShell Core](https://github.com/PowerShell/PowerShell) | 7+ (`pwsh`) | Must be on PATH |
| [Docker](https://www.docker.com/) | 24+ | Engine must be running; PostgreSQL runs in a container |

All four are required for `./test.ps1 -Tier all`. The `unit` and `api` tiers only need .NET SDK.

---

## Quick start

```powershell
# From the repository root:
pwsh ./test.ps1 -Tier all
```

That one command:
1. Starts the PostgreSQL container (`docker-compose.tests.yml`) if it is not already running.
2. Runs the **unit** tier (framework + example unit tests).
3. Runs the **api** tier (in-process API E2E, SQLite in-memory — no infrastructure needed).
4. Runs the **ui** tier: resets the dedicated `mindedexample_e2e` database, starts the API and
   Vite dev server automatically, runs Playwright in Chromium headless.
5. Prints a per-project pass/fail summary and exits non-zero on any failure.

---

## Tiers in detail

### `unit` — framework and unit tests

```powershell
pwsh ./test.ps1 -Tier unit
```

- **Projects**: everything under `Tests/` (`*.Tests.csproj`) + example unit tests
  (`Example/Tests/**/*UnitTests.csproj`).
- **Infrastructure**: none.
- **Typical duration**: 30–90 s.

### `api` — in-process API E2E tests

```powershell
pwsh ./test.ps1 -Tier api                    # SQLite in-memory (default, fastest)
pwsh ./test.ps1 -Tier api -ApiDatabase postgres  # PostgreSQL (requires Docker)
```

- **Projects**: `Example/Tests/MindedExample.Api.E2ETests` and `*.IntegrationTests` projects.
- **Infrastructure**: none for SQLite; Docker PostgreSQL for `-ApiDatabase postgres`.
- **Typical duration**: 1–2 min (SQLite), 3–5 min (PostgreSQL — each test creates its own DB).

PostgreSQL mode sets `MINDEDTEST_TestingProfile=E2E` and `MINDEDTEST_DatabaseType=PostgreSQL`
environment variables, which are read by `BaseE2ETest`. Each run uses a unique database
(`minded_apitests_<run-id>`) that is dropped automatically on teardown.

### `ui` — Playwright browser E2E tests

```powershell
pwsh ./test.ps1 -Tier ui
```

- **Projects**: `Example/Tests/e2e-ui` (`@playwright/test`, Chromium).
- **Infrastructure**: Docker PostgreSQL (`docker-compose.tests.yml` default profile —
  postgres only). The API and Vite dev server are started automatically by Playwright's
  `webServer` config.
- **Database**: dedicated `mindedexample_e2e` database, reset before every run by the
  `pretest` npm script (`scripts/reset-db.mjs`). The API recreates the schema and seeds
  reference data on boot.
- **Typical duration**: 2–4 min (includes API + Vite startup).

#### Running against a containerized stack (optional)

The `--profile full` variant of the compose file adds the API and frontend as Docker services,
removing the Node.js and .NET runtime requirements from the test machine:

```powershell
$env:MINDED_DB = 'mindedexample_e2e'
docker compose -f docker-compose.tests.yml --profile full up -d --build

# Wait for containers to be healthy, then:
$env:BASE_URL = 'http://localhost:3000'
cd Example/Tests/e2e-ui
npx playwright test
```

> **Note**: When using `BASE_URL`, the `npm pretest` DB reset is **bypassed** because the
> API is already running. Reset the database manually between runs if needed:
> ```powershell
> docker exec minded-postgres psql -U minded -d postgres -c "DROP DATABASE IF EXISTS mindedexample_e2e;"
> docker exec minded-postgres psql -U minded -d postgres -c "CREATE DATABASE mindedexample_e2e;"
> docker restart minded-api
> ```

---

## Artifact locations

All results land under `TestResults/` at the repository root.

| Tier | Artifact type | Path |
|------|--------------|------|
| unit | TRX (MSTest) | `TestResults/unit/<project>.trx` |
| unit | Cobertura coverage | `TestResults/unit/<run-id>/coverage.cobertura.xml` |
| api | TRX (MSTest) | `TestResults/api/<project>.trx` |
| api | Cobertura coverage | `TestResults/api/<run-id>/coverage.cobertura.xml` |
| ui | JUnit XML | `TestResults/ui/junit.xml` |
| ui | Playwright HTML report | `TestResults/ui/html/index.html` |
| ui | Traces (on first retry) | `TestResults/ui/html/trace/` |
| ui | Screenshots (on failure) | `TestResults/ui/html/data/` |
| ui | Videos (on failure) | `TestResults/ui/html/data/` |

Open the Playwright HTML report locally:

```powershell
cd Example/Tests/e2e-ui && npm run report
```

---

## Environment variables

| Variable | Tier | Default | Purpose |
|----------|------|---------|---------|
| `MINDEDTEST_TestingProfile` | api | `SQLiteInMemory` | `E2E` activates the PostgreSQL profile |
| `MINDEDTEST_DatabaseType` | api | `SQLiteInMemory` | `PostgreSQL` switches the API E2E db |
| `BASE_URL` | ui | _(unset)_ | When set, Playwright targets this URL instead of starting local servers |
| `MINDED_DB` | ui (compose) | `mindedexample` | PostgreSQL database name used by the containerized API |
| `CI` | ui | _(unset)_ | Set to any value in CI pipelines; disables `reuseExistingServer` and enables one retry per test |

---

## Suggested CI pipeline shape

> No CI pipeline file is committed. The steps below map directly to `test.ps1`.

```
pipeline:
  services:
    - postgres:17-alpine           # or: docker compose -f docker-compose.tests.yml up -d

  steps:
    - name: Restore
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore --configuration Release

    - name: Unit tests
      run: pwsh ./test.ps1 -Tier unit -Configuration Release
      artifacts: TestResults/unit/**

    - name: API E2E tests (SQLite)
      run: pwsh ./test.ps1 -Tier api -Configuration Release
      artifacts: TestResults/api/**

    - name: API E2E tests (PostgreSQL)       # optional second pass
      run: pwsh ./test.ps1 -Tier api -ApiDatabase postgres -Configuration Release
      artifacts: TestResults/api/**

    - name: Install Playwright browsers
      run: |
        cd Example/Tests/e2e-ui
        npm ci
        npx playwright install --with-deps chromium

    - name: UI E2E tests
      run: pwsh ./test.ps1 -Tier ui -Configuration Release
      env:
        CI: "true"
      artifacts:
        - TestResults/ui/junit.xml
        - TestResults/ui/html/**
```

**Sequencing notes**:
- `unit` and `api` can run in parallel if the CI platform supports it; `ui` depends on
  nothing but needs Docker.
- The `api` SQLite pass is fast (< 2 min) and has no infra dependency — ideal as an early
  gate before the longer PostgreSQL / UI tiers.
- Set `CI=true` for the `ui` step: it enables one automatic retry per flaky test and
  disables server reuse (forces a fresh API + Vite startup each run).

---

## Troubleshooting

### Docker not found / engine not running

`test.ps1` attempts to start Docker Desktop automatically and waits 2 minutes. If it still
fails, start Docker Desktop manually before re-running.

### Port 6000 already in use

The Playwright `webServer` config uses `reuseExistingServer: !process.env.CI`. Locally, if a
process is already occupying port 6000 (e.g. a leftover Docker container or a background
`dotnet run`), Playwright will reuse it *and* the `pretest` DB reset will be skipped (because
the healthcheck responds). Either stop the conflicting process or set `CI=true` to force a
fresh start.

### UI tests fail with "element not visible" after many runs

The test database accumulates entity rows across repeated local runs. The `pretest` reset
handles this automatically when `npm test` / `./test.ps1 -Tier ui` is invoked normally. If
tests were run via `npx playwright test` directly (which bypasses `pretest`), reset manually:

```powershell
cd Example/Tests/e2e-ui && node scripts/reset-db.mjs
```

### PostgreSQL container not starting

```powershell
docker compose -f docker-compose.tests.yml up -d --wait
docker logs minded-postgres
```

Check for port conflicts on 5433. Modify `docker-compose.tests.yml` if another local Postgres
occupies 5433.
