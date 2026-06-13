import { test, expect } from '@playwright/test';

/**
 * Walking-skeleton smoke tests: prove the auto-started stack (API + Vite + PostgreSQL)
 * serves the frontend and that a real end-to-end login round trip works.
 */
test.describe('smoke', () => {
  test('login page renders', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByRole('textbox', { name: 'Email' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Password' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible();
  });

  test('global admin can sign in and lands on tenant management', async ({ page }) => {
    await page.goto('/login');

    await page.getByRole('textbox', { name: 'Email' }).fill('admin@example.com');
    await page.getByRole('textbox', { name: 'Password' }).fill('Admin1!');
    await page.getByRole('button', { name: 'Sign In' }).click();

    await expect(page).toHaveURL(/\/admin\/global-tenants/, { timeout: 15_000 });
  });
});
