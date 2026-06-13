import { test, expect } from '../helpers/fixtures';
import { api, AuthSession, uniqueName } from '../helpers/api';
import { E2E_PASSWORD } from '../helpers/personas';
import { clickDialogButton, dialog, expectToast, gridRow } from '../helpers/ui';
import type { Page } from '@playwright/test';

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}@example.com`.toLowerCase();
}

async function createTenantViaUi(page: Page, tenantName: string, ownerEmail: string): Promise<void> {
  await page.goto('/admin/global-tenants');
  await page.getByRole('textbox', { name: 'Tenant Name' }).fill(tenantName);
  await page.getByRole('textbox', { name: 'Owner First Name' }).fill('Legal');
  await page.getByRole('textbox', { name: 'Owner Surname' }).fill('Owner');
  await page.getByRole('textbox', { name: 'Owner Email' }).fill(ownerEmail);
  await page.getByRole('textbox', { name: 'Owner Password' }).fill(E2E_PASSWORD);
  await page.getByRole('button', { name: 'Create Tenant' }).click();
  await expectToast(page, 'Tenant created successfully.');

  // Sort by ID descending so the just-created row (highest ID) appears at the top of
  // the fixed-height DataGrid viewport regardless of its name.
  const idHeader = page.getByRole('columnheader', { name: 'ID' });
  await idHeader.click();
  await idHeader.click();
}

test.describe('global tenant management', () => {
  test('tenant summaries are listed for the global admin', async ({ pageAsGlobalAdmin, personas }) => {
    await pageAsGlobalAdmin.goto('/admin/global-tenants');

    const row = gridRow(pageAsGlobalAdmin, personas.ownerA.Tenant!.Name);
    await expect(row).toBeVisible();
    await expect(row).toContainText(personas.ownerA.User.Email);
  });

  test('creating a tenant produces a legal owner who can sign in', async ({ pageAsGlobalAdmin }) => {
    const tenantName = uniqueName('GT Tenant');
    const ownerEmail = uniqueEmail('e2e-gt-owner');

    await createTenantViaUi(pageAsGlobalAdmin, tenantName, ownerEmail);
    await expect(gridRow(pageAsGlobalAdmin, tenantName)).toBeVisible();

    const login = await api.raw('POST', '/auth/login', { Email: ownerEmail, Password: E2E_PASSWORD });
    expect(login.ok).toBe(true);
    expect(login.json<AuthSession>().Tenant?.Name).toBe(tenantName);
  });

  test('deletion requires the exact tenant name and then removes everything', async ({ pageAsGlobalAdmin }) => {
    const tenantName = uniqueName('GT Doomed');
    const ownerEmail = uniqueEmail('e2e-gt-doomed');
    await createTenantViaUi(pageAsGlobalAdmin, tenantName, ownerEmail);

    // Wrong confirmation name blocks the deletion; the error shows inside the still-open dialog
    await gridRow(pageAsGlobalAdmin, tenantName).getByRole('button', { name: 'Delete' }).click();
    await dialog(pageAsGlobalAdmin).getByRole('textbox', { name: 'Tenant name confirmation' }).fill('Wrong Name');
    await clickDialogButton(pageAsGlobalAdmin, 'Delete');
    await expect(dialog(pageAsGlobalAdmin).getByRole('alert')).toContainText('Failed to delete tenant');

    // The tenant survived (the grid is only role-addressable once the modal closes)
    await clickDialogButton(pageAsGlobalAdmin, 'Cancel');
    await expect(gridRow(pageAsGlobalAdmin, tenantName)).toBeVisible();

    // Correct confirmation deletes the tenant
    await gridRow(pageAsGlobalAdmin, tenantName).getByRole('button', { name: 'Delete' }).click();
    await dialog(pageAsGlobalAdmin).getByRole('textbox', { name: 'Tenant name confirmation' }).fill(tenantName);
    await clickDialogButton(pageAsGlobalAdmin, 'Delete');
    await expectToast(pageAsGlobalAdmin, 'Tenant deleted successfully.');
    await expect(gridRow(pageAsGlobalAdmin, tenantName)).toHaveCount(0);

    // The tenant's owner can no longer sign in
    const login = await api.raw('POST', '/auth/login', { Email: ownerEmail, Password: E2E_PASSWORD });
    expect(login.status).toBe(401);
  });

  test('tenant owners have no access to global tenant management', async ({ pageAsOwnerA }) => {
    // No navigation entry...
    await pageAsOwnerA.goto('/');
    await expect(pageAsOwnerA.getByRole('button', { name: 'Tenants', exact: true })).toHaveCount(0);

    // ...and the API rejects direct access (the page surfaces the failure)
    await pageAsOwnerA.goto('/admin/global-tenants');
    await expect(pageAsOwnerA.getByRole('alert').filter({ hasText: /Failed to load/ })).toBeVisible();
  });
});
