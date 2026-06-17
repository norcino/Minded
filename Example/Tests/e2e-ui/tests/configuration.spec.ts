import { test, expect } from '../helpers/fixtures';
import type { Page } from '@playwright/test';

/**
 * Runtime configuration page (/configuration).
 * Reads require authentication only; updates require the CanUpdateConfiguration
 * permission (TenantAdmin role) — see ConfigurationsController / T-12.
 * Feedback is shown in inline alerts; value controls are addressed by their
 * configuration key (aria-label) and per-entry reset buttons by "Reset <key>".
 */

const RETRY_COUNT_KEY = 'Retry.DefaultRetryCount';

async function expandRetryOptions(page: Page): Promise<void> {
  await page.getByRole('button', { name: /Retry Options/ }).click();
}

test.describe('runtime configuration', () => {
  test('configuration options are listed grouped by category', async ({ pageAsOwnerA }) => {
    await pageAsOwnerA.goto('/configuration');

    await expect(pageAsOwnerA.getByRole('heading', { name: 'Runtime Configuration' })).toBeVisible();
    for (const category of ['System Options', 'Logging Options', 'Retry Options']) {
      await expect(pageAsOwnerA.getByRole('button', { name: new RegExp(category) })).toBeVisible();
    }

    // Default-expanded categories show their entries with name, description and control
    await expect(pageAsOwnerA.getByText('Enables or disables logging decorator.', { exact: false })).toBeVisible();
    await expect(pageAsOwnerA.getByRole('group', { name: 'Logging.Enabled' }).getByRole('switch')).toBeVisible();
  });

  test('an integer option can be edited, persists across reload, and resets to default', async ({ pageAsOwnerA }) => {
    await pageAsOwnerA.goto('/configuration');
    await expandRetryOptions(pageAsOwnerA);

    const field = pageAsOwnerA.getByRole('spinbutton', { name: RETRY_COUNT_KEY });
    await field.fill('5');
    await expect(pageAsOwnerA.getByRole('alert').filter({ hasText: `Updated ${RETRY_COUNT_KEY}` })).toBeVisible();

    // Survives a reload (server-side runtime state, not client state)
    await pageAsOwnerA.reload();
    await expandRetryOptions(pageAsOwnerA);
    await expect(field).toHaveValue('5');

    // Reset restores the documented default and disables itself
    const reset = pageAsOwnerA.getByRole('button', { name: `Reset ${RETRY_COUNT_KEY}` });
    await reset.click();
    await expect(pageAsOwnerA.getByRole('alert').filter({ hasText: `Reset ${RETRY_COUNT_KEY} to default` })).toBeVisible();
    await expect(field).toHaveValue('3');
    await expect(reset).toBeDisabled();
  });

  test('a non-numeric value is rejected with a visible error and not persisted', async ({ pageAsOwnerA }) => {
    await pageAsOwnerA.goto('/configuration');
    await expandRetryOptions(pageAsOwnerA);

    const field = pageAsOwnerA.getByRole('spinbutton', { name: RETRY_COUNT_KEY });
    await field.fill('');
    await expect(pageAsOwnerA.getByRole('alert').filter({ hasText: 'Invalid number value for DefaultRetryCount' })).toBeVisible();

    // The controlled field snaps back to the stored value; nothing was sent to the API
    await pageAsOwnerA.reload();
    await expandRetryOptions(pageAsOwnerA);
    await expect(field).toHaveValue('3');
  });

  test('a plain member can view configurations but updates are rejected', async ({ pageAsMemberA }) => {
    await pageAsMemberA.goto('/configuration');

    const toggle = pageAsMemberA.getByRole('group', { name: 'Logging.LogOutcomeEntries' }).getByRole('switch');
    await expect(toggle).not.toBeChecked();
    await toggle.click();

    await expect(pageAsMemberA.getByRole('alert').filter({ hasText: /Request failed|Failed to update/ })).toBeVisible();

    // The denied update did not stick
    await pageAsMemberA.reload();
    await expect(pageAsMemberA.getByRole('group', { name: 'Logging.LogOutcomeEntries' }).getByRole('switch')).not.toBeChecked();
  });

  test('the page requires authentication', async ({ page }) => {
    await page.goto('/configuration');
    await expect(page).toHaveURL(/\/login/);
  });
});
