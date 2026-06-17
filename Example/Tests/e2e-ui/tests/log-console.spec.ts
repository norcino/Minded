import { test, expect } from '../helpers/fixtures';
import { createCategory, uniqueName } from '../helpers/api';

/**
 * Smoke test for the real-time log console (SignalR over the /hubs/logs websocket,
 * proxied by Vite). Assertions are deliberately loose — log content is not a contract;
 * what matters is that entries arrive end to end with a level and a message.
 */
test.describe('log console', () => {
  test('api activity streams into the log console over SignalR', async ({ pageAsOwnerA, personas }) => {
    // Any authenticated page hosts the console (fixed at the bottom of the layout)
    await pageAsOwnerA.goto('/categories');

    // The websocket connection is established ("Disconnected" also contains "Connected" — exact match)
    await expect(pageAsOwnerA.getByText('Connected', { exact: true })).toBeVisible({ timeout: 15_000 });

    const logRegion = pageAsOwnerA.getByRole('log', { name: 'Application logs' });
    await expect(logRegion).toBeVisible();

    // Trigger real API activity; the logging decorator echoes the command name in the log stream
    // (the entity name is redacted by sensitive-data masking, so we assert on the command instead)
    await createCategory(personas.ownerA, uniqueName('LogSmoke'));

    // Entries arrive within a timeout and carry a level and the triggering activity's command name
    await expect(logRegion.getByText(/CreateCategoryCommand/).first()).toBeVisible({ timeout: 15_000 });
    await expect(logRegion.getByText(/\[(VERBOSE|DEBUG|INFORMATION|WARNING|ERROR|FATAL)\]/).first()).toBeVisible();

    // Clear empties the console back to its idle state
    await pageAsOwnerA.getByRole('button', { name: 'Clear Logs' }).click();
    await expect(logRegion.getByText('Waiting for logs...')).toBeVisible();
  });
});
