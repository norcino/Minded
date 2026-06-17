import { test, expect } from '../helpers/fixtures';
import { registerTenantOwner, uniqueName } from '../helpers/api';
import { getLatestPasswordResetToken } from '../helpers/db';
import { E2E_PASSWORD } from '../helpers/personas';
import { loginViaForm } from '../helpers/ui';

test.describe('password reset', () => {
  test('full cycle: forgot, reset with the issued token, login with the new password', async ({ page }) => {
    const email = `e2e-reset-${Date.now()}@example.com`;
    const newPassword = 'NewPassw0rd!';
    await registerTenantOwner(email, E2E_PASSWORD, uniqueName('Reset Tenant'));

    // Request the reset
    await page.goto('/forgot-password');
    await page.getByRole('textbox', { name: 'Email' }).fill(email);
    await page.getByRole('button', { name: 'Send Reset Link' }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'If the account exists' })).toBeVisible();

    // The token is delivered by email in a real deployment; read it from the database
    const token = await getLatestPasswordResetToken(email);
    expect(token).toBeTruthy();

    // Set the new password through the reset page
    await page.goto(`/reset-password?token=${token}`);
    await page.getByRole('textbox', { name: 'New Password' }).fill(newPassword);
    await page.getByRole('button', { name: 'Reset Password' }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'Password updated successfully' })).toBeVisible();

    // Old password is rejected, new one signs in
    await loginViaForm(page, email, E2E_PASSWORD);
    await expect(page.getByRole('alert').filter({ hasText: 'Invalid email or password.' })).toBeVisible();

    await loginViaForm(page, email, newPassword);
    await expect(page).toHaveURL(/\/$/);
  });

  test('unknown email shows the same neutral confirmation (no account enumeration)', async ({ page }) => {
    await page.goto('/forgot-password');
    await page.getByRole('textbox', { name: 'Email' }).fill(`nobody-${Date.now()}@example.com`);
    await page.getByRole('button', { name: 'Send Reset Link' }).click();

    await expect(page.getByRole('alert').filter({ hasText: 'If the account exists' })).toBeVisible();
  });

  test('an invalid token shows an error state', async ({ page }) => {
    await page.goto('/reset-password?token=not-a-real-token');
    await page.getByRole('textbox', { name: 'New Password' }).fill('Whatever1!');
    await page.getByRole('button', { name: 'Reset Password' }).click();

    await expect(page.getByRole('alert').filter({ hasText: 'invalid or expired' })).toBeVisible();
  });
});
