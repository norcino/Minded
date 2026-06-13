import { test, expect } from '../helpers/fixtures';
import { E2E_PASSWORD } from '../helpers/personas';
import { loginViaForm, logout } from '../helpers/ui';

const PROTECTED_ROUTES = [
  '/',
  '/categories',
  '/transactions',
  '/configuration',
  '/admin/roles',
  '/admin/user-roles',
  '/admin/tenant',
  '/admin/global-tenants',
];

test.describe('authentication', () => {
  test('tenant owner login lands on the home page with navigation', async ({ page, personas }) => {
    await loginViaForm(page, personas.ownerA.User.Email, E2E_PASSWORD);

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('button', { name: 'Categories', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Logout' })).toBeVisible();
  });

  test('member login lands on the home page', async ({ page, personas }) => {
    await loginViaForm(page, personas.memberA.User.Email, E2E_PASSWORD);

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('button', { name: 'Logout' })).toBeVisible();
  });

  test('global admin login is redirected to global tenant management', async ({ page, personas }) => {
    await loginViaForm(page, personas.globalAdmin.User.Email, 'Admin1!');

    await expect(page).toHaveURL(/\/admin\/global-tenants/);
  });

  test('invalid password shows an error and stays on the login page', async ({ page, personas }) => {
    await loginViaForm(page, personas.ownerA.User.Email, 'WrongPassword1!');

    await expect(page.getByRole('alert').filter({ hasText: 'Invalid email or password.' })).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test('every protected route redirects anonymous visitors to login', async ({ page }) => {
    for (const route of PROTECTED_ROUTES) {
      await page.goto(route);
      await expect(page, `route ${route} must be guarded`).toHaveURL(/\/login/);
    }
  });

  test('logout clears the session and back navigation cannot re-enter', async ({ page, personas }) => {
    await loginViaForm(page, personas.ownerA.User.Email, E2E_PASSWORD);
    await expect(page).toHaveURL(/\/$/);

    await logout(page);

    // Browser back must not resurrect the authenticated session
    await page.goBack();
    await expect(page).toHaveURL(/\/login/);

    // And protected routes stay guarded
    await page.goto('/categories');
    await expect(page).toHaveURL(/\/login/);
  });

  test('deep links are not restored after login (current behavior: home redirect)', async ({ page, personas }) => {
    // Visiting a protected route anonymously bounces to /login...
    await page.goto('/transactions');
    await expect(page).toHaveURL(/\/login/);

    // ...and after signing in the app navigates home rather than back to /transactions.
    // RequireAuth passes the original location in state, but LoginPage does not use it.
    await page.getByRole('textbox', { name: 'Email' }).fill(personas.ownerA.User.Email);
    await page.getByRole('textbox', { name: 'Password' }).fill(E2E_PASSWORD);
    await page.getByRole('button', { name: 'Sign In' }).click();

    await expect(page).toHaveURL(/\/$/);
  });
});
