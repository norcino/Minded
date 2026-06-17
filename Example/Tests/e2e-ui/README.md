# Minded Example — UI E2E tests (Playwright)

Browser-level end-to-end tests for the Example application: real frontend (Vite + React),
real API (ASP.NET Core), real database (PostgreSQL).

## Prerequisites

- Node.js 20+
- .NET 10 SDK
- Docker (PostgreSQL container): `docker compose -f ../../../docker-compose.tests.yml up -d`

## Running

```bash
npm ci
npx playwright install chromium   # first time only
npm test                          # or: pwsh ../../../test.ps1 -Tier ui  (from the repo root)
```

`npm test` does everything else automatically:

1. **Database reset** (`scripts/reset-db.mjs`, via `pretest`): drops the schema of the dedicated
   `mindedexample_e2e` database so the API recreates and seeds it at startup. Skipped when an
   API instance is already running (local iteration with `reuseExistingServer`).
2. **Stack startup** (`webServer` in `playwright.config.ts`): the API on `http://localhost:6000`
   (PostgreSQL profile) and the Vite dev server on `http://localhost:3000`.
3. Tests run in Chromium, single worker (one shared database).

Reports land in `TestResults/ui/` at the repository root (HTML + JUnit); traces, screenshots
and videos are captured on failure. `npm run test:ui` opens Playwright's interactive UI mode.

## Conventions

- **Arrange through the API** (`helpers/api.ts`): login, register tenants, create
  categories/transactions. Direct database access (`helpers/db.ts`) is only for state with no
  API surface (password-reset tokens, invite tokens — normally delivered by email).
- **Unique test data**: tests create uniquely-named entities (`uniqueName(...)`) instead of
  truncating between tests — the database persists for the whole run.
- **Seeded accounts** (created by the application's development seeder):
  `admin@example.com` (global admin), `admin-tenant1@example.com`, `admin-tenant2@example.com`
  — all with password `Admin1!`.
- **Selectors**: prefer `getByRole`/accessible names, via the shared helpers in
  `helpers/ui.ts` (navigation, DataGrid rows/actions, dialogs, toasts). Icon-only controls
  in the frontend carry `aria-label` attributes — when adding new icon-only buttons, give
  them an `aria-label` rather than introducing `data-testid`. Notes:
  - MUI renders required-field labels as `Label *`, so `getByLabel('X', { exact: true })`
    does NOT match — use `getByRole('textbox', { name: 'X' })` instead.
  - MUI `Tooltip` does not provide an accessible name; icon buttons need explicit `aria-label`.
- **Personas** (`helpers/fixtures.ts`): `pageAsGlobalAdmin`, `pageAsOwnerA`, `pageAsOwnerB`,
  `pageAsMemberA` are pre-authenticated pages; the plain `page` fixture is anonymous.
  Identities/API sessions are available through the `personas` fixture.
- The API serializes JSON in **PascalCase**; API helper types use the same casing.
