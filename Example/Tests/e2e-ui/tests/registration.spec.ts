import { test, expect } from '../helpers/fixtures';
import { api, login, uniqueName } from '../helpers/api';
import { E2E_PASSWORD } from '../helpers/personas';

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}@example.com`;
}

test.describe('registration', () => {
  test('create-tenant mode signs the new owner in with an isolated empty tenant', async ({ page }) => {
    const email = uniqueEmail('e2e-reg-owner');
    const tenantName = uniqueName('Reg Tenant');

    await page.goto('/register');
    await page.getByRole('textbox', { name: 'First name' }).fill('Reg');
    await page.getByRole('textbox', { name: 'Surname' }).fill('Owner');
    await page.getByRole('textbox', { name: 'Tenant name' }).fill(tenantName);
    await page.getByRole('textbox', { name: 'Email' }).fill(email);
    await page.getByRole('textbox', { name: 'Password' }).fill(E2E_PASSWORD);
    await page.getByRole('button', { name: 'Create Account' }).click();

    // Auto-login lands on the home page as the new user
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByText(/Signed in as: Reg Owner/)).toBeVisible();

    // After a reload the persisted session shows the new tenant in the app bar
    await page.reload();
    await expect(page.getByText(`(${tenantName})`)).toBeVisible();

    // The fresh tenant is empty: no demo/foreign categories are visible to its owner
    const session = await login(email, E2E_PASSWORD);
    const categories = await api.get<unknown[]>('/category', session);
    expect(categories).toHaveLength(0);
  });

  test('join-tenant mode submits a pending request visible to the tenant owner', async ({ page, personas }) => {
    const email = uniqueEmail('e2e-reg-joiner');

    await page.goto('/register');
    await page.getByRole('combobox', { name: 'Registration Type' }).click();
    await page.getByRole('option', { name: 'Join an existing tenant' }).click();

    await page.getByRole('textbox', { name: 'First name' }).fill('Reg');
    await page.getByRole('textbox', { name: 'Surname' }).fill('Joiner');
    await page.getByRole('textbox', { name: 'Tenant name' }).fill(personas.ownerA.Tenant!.Name);
    await page.getByRole('textbox', { name: 'Email' }).fill(email);
    await page.getByRole('textbox', { name: 'Password' }).fill(E2E_PASSWORD);
    await page.getByRole('button', { name: 'Submit Request' }).click();

    // Pending-approval message is shown and the visitor stays unauthenticated
    await expect(page.getByRole('alert').filter({ hasText: /must approve your request/i })).toBeVisible();
    await expect(page).toHaveURL(/\/register/);

    // The request shows up in the target tenant's pending list
    const pending = await api.get<Array<{ Email: string }>>('/tenant-admin/join-requests', personas.ownerA);
    expect(pending.some(r => r.Email === email.toLowerCase())).toBe(true);
  });

  test('registering with an already used email shows a friendly error', async ({ page, personas }) => {
    await page.goto('/register');
    await page.getByRole('textbox', { name: 'First name' }).fill('Dup');
    await page.getByRole('textbox', { name: 'Surname' }).fill('User');
    await page.getByRole('textbox', { name: 'Tenant name' }).fill(uniqueName('Dup Tenant'));
    await page.getByRole('textbox', { name: 'Email' }).fill(personas.ownerA.User.Email);
    await page.getByRole('textbox', { name: 'Password' }).fill(E2E_PASSWORD);
    await page.getByRole('button', { name: 'Create Account' }).click();

    await expect(page.getByRole('alert').filter({ hasText: 'Failed to register' })).toBeVisible();
    await expect(page).toHaveURL(/\/register/);
  });
});
