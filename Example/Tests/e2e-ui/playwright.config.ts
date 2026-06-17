import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for the Minded Example UI E2E tests.
 *
 * The full application stack is started automatically:
 *  - API (dotnet run) on http://localhost:6000, backed by the dedicated PostgreSQL
 *    database `mindedexample_e2e` from docker-compose.tests.yml (host port 5433).
 *  - Frontend (Vite dev server) on http://localhost:3000, proxying /api and /hubs to the API.
 *
 * Prerequisite: the PostgreSQL container must be running
 * (docker compose -f docker-compose.tests.yml up -d). The test.ps1 `ui` tier does this for you.
 *
 * Alternatively, set BASE_URL to target an externally managed stack (e.g. the fully
 * containerized one: docker compose -f docker-compose.tests.yml --profile full up with
 * MINDED_DB=mindedexample_e2e) — no servers are started in that case. The stack must
 * expose the frontend at BASE_URL and the API at http://localhost:6000 (the compose
 * `full` profile maps both).
 */
const externalBaseUrl = process.env.BASE_URL;

export default defineConfig({
  testDir: './tests',
  // The stack shares one database: keep a single worker until per-test data isolation
  // strategies (unique entities per test) prove parallel-safe.
  workers: 1,
  fullyParallel: false,
  retries: process.env.CI ? 1 : 0,
  timeout: 60_000,
  reporter: [
    ['list'],
    ['html', { outputFolder: '../../../TestResults/ui/html', open: 'never' }],
    ['junit', { outputFile: '../../../TestResults/ui/junit.xml' }],
  ],
  use: {
    baseURL: externalBaseUrl ?? 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      // Provisions personas (accounts + storage states) once per run
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // Tall viewport (after the devices spread, which would otherwise reset it):
        // the app's fixed LogConsole covers the bottom ~300px of the window and would
        // intercept clicks on grid footers/pagination.
        viewport: { width: 1280, height: 1600 },
      },
      dependencies: ['setup'],
      testIgnore: /.*\.setup\.ts/,
    },
  ],
  webServer: externalBaseUrl ? [] : [
    {
      command: 'dotnet run --project ../../MindedExample.Api/MindedExample.Api.csproj',
      url: 'http://localhost:6000/api/healthcheck',
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        DatabaseType: 'PostgreSQL',
        ConnectionStrings__MindedExamplePostgreSQL:
          'Host=localhost;Port=5433;Database=mindedexample_e2e;Username=minded;Password=minded',
      },
    },
    {
      command: 'npm run dev',
      cwd: '../../Frontend',
      url: 'http://localhost:3000',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
  ],
});
