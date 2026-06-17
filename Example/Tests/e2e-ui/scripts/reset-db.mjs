// Resets the dedicated UI E2E database (mindedexample_e2e) so every run starts from a
// known state: the schema is dropped here, and the API recreates and seeds it at startup
// (EnsureCreated + DatabaseSeeder run when the application boots in Development).
//
// Runs as the npm "pretest" script because Playwright starts webServer processes BEFORE
// globalSetup: the reset must happen before the API boots. When an API instance is already
// running (reuseExistingServer during local iteration), the reset is skipped — dropping the
// schema under a live application would break it; tests create uniquely-named data instead.
import { Client } from 'pg';
import http from 'node:http';

// node:http instead of fetch: port 6000 is on the fetch spec's "bad ports" list (X11)
// and undici refuses to connect to it.
const apiIsRunning = await new Promise(resolve => {
  const req = http.get('http://localhost:6000/api/healthcheck', { timeout: 2000 }, res => {
    res.resume();
    resolve(res.statusCode === 200);
  });
  req.on('error', () => resolve(false));
  req.on('timeout', () => { req.destroy(); resolve(false); });
});

if (apiIsRunning) {
  console.log('[reset-db] API already running (reuse mode) - skipping database reset');
  process.exit(0);
}

const client = new Client({
  host: 'localhost',
  port: 5433,
  database: 'mindedexample_e2e',
  user: 'minded',
  password: 'minded',
});

try {
  await client.connect();
} catch (error) {
  console.error('[reset-db] Cannot reach PostgreSQL on localhost:5433.');
  console.error('[reset-db] Start it with: docker compose -f docker-compose.tests.yml up -d');
  console.error(`[reset-db] ${error.message}`);
  process.exit(1);
}

await client.query('DROP SCHEMA IF EXISTS dbo CASCADE');
await client.query('DROP SCHEMA IF EXISTS public CASCADE');
await client.query('CREATE SCHEMA public');
await client.end();
console.log('[reset-db] mindedexample_e2e schema reset - the API will recreate and seed it at startup');
