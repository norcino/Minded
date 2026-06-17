import { test, expect } from '../helpers/fixtures';
import { logout, navigateTo } from '../helpers/ui';

/**
 * Validates the persona fixtures: each pre-authenticated page lands on its expected
 * default route without going through the login form, and anonymous visitors are
 * redirected to /login by the route guard.
 */
test.describe('personas', () => {
  test('global admin fixture lands on global tenant management', async ({ pageAsGlobalAdmin }) => {
    await pageAsGlobalAdmin.goto('/');
    await expect(pageAsGlobalAdmin).toHaveURL(/\/admin\/global-tenants/);
  });

  test('tenant owner fixture is authenticated and can navigate and log out', async ({ pageAsOwnerA }) => {
    await pageAsOwnerA.goto('/');
    await expect(pageAsOwnerA).not.toHaveURL(/\/login/);

    // Exercises the shared UI helpers: sidebar navigation and app-bar logout
    await navigateTo(pageAsOwnerA, 'Categories', /\/categories/);
    await logout(pageAsOwnerA);
  });

  test('member fixture is authenticated on the home page', async ({ pageAsMemberA }) => {
    await pageAsMemberA.goto('/');
    await expect(pageAsMemberA).not.toHaveURL(/\/login/);
  });

  test('anonymous visitors are redirected to login by the route guard', async ({ page }) => {
    await page.goto('/categories');
    await expect(page).toHaveURL(/\/login/);
  });
});
