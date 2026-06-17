import { test, expect } from '../helpers/fixtures';
import { createCategory, createTransaction, uniqueName } from '../helpers/api';
import { gridRow } from '../helpers/ui';
import type { Page } from '@playwright/test';

/**
 * UI mirror of the T-13 API isolation suite: two tenants drive the app in parallel
 * browser contexts and never see each other's data. Note: the frontend has no
 * entity-id deep-link routes (all entity access goes through tenant-scoped grids),
 * so the "deep-link a foreign id" scenario from the roadmap has no UI surface to test —
 * the API side of it (404 on direct foreign-id access) is covered by TenantIsolationE2ETests.
 */

async function openCategoriesGrid(page: Page): Promise<void> {
  await page.goto('/categories');
  await page.getByRole('button', { name: 'Grid view' }).click();
  await expect(page.getByRole('grid')).toBeVisible();

  // Newest-first: fresh rows land on page 1, so a leaked foreign row would be visible
  await page.getByRole('columnheader', { name: 'ID' }).click();
  await page.getByRole('columnheader', { name: 'ID' }).click();
}

async function openTransactionsGrid(page: Page): Promise<void> {
  await page.goto('/transactions');
  await expect(page.getByRole('grid')).toBeVisible();
  await page.getByRole('columnheader', { name: 'ID' }).click();
  await page.getByRole('columnheader', { name: 'ID' }).click();
}

test.describe('multi-tenant isolation', () => {
  test('category grids of two tenants are disjoint', async ({ pageAsOwnerA, pageAsOwnerB, personas }) => {
    const nameA = uniqueName('IsoCatA');
    const nameB = uniqueName('IsoCatB');
    await createCategory(personas.ownerA, nameA);
    await createCategory(personas.ownerB, nameB);

    await openCategoriesGrid(pageAsOwnerA);
    await expect(gridRow(pageAsOwnerA, nameA)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, nameB)).toHaveCount(0);

    await openCategoriesGrid(pageAsOwnerB);
    await expect(gridRow(pageAsOwnerB, nameB)).toBeVisible();
    await expect(gridRow(pageAsOwnerB, nameA)).toHaveCount(0);
  });

  test('transaction grids of two tenants are disjoint', async ({ pageAsOwnerA, pageAsOwnerB, personas }) => {
    const txA = uniqueName('IsoTxA');
    const txB = uniqueName('IsoTxB');
    const catA = await createCategory(personas.ownerA, uniqueName('IsoTxCatA'));
    const catB = await createCategory(personas.ownerB, uniqueName('IsoTxCatB'));
    await createTransaction(personas.ownerA, catA.Id, txA);
    await createTransaction(personas.ownerB, catB.Id, txB);

    await openTransactionsGrid(pageAsOwnerA);
    await expect(gridRow(pageAsOwnerA, txA)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, txB)).toHaveCount(0);

    await openTransactionsGrid(pageAsOwnerB);
    await expect(gridRow(pageAsOwnerB, txB)).toBeVisible();
    await expect(gridRow(pageAsOwnerB, txA)).toHaveCount(0);
  });

  test('user lists and tenant-admin member lists are tenant-scoped', async ({ pageAsOwnerA, pageAsOwnerB, personas }) => {
    // Home page user grid
    await pageAsOwnerA.goto('/');
    await expect(gridRow(pageAsOwnerA, personas.ownerA.User.Email)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, personas.ownerB.User.Email)).toHaveCount(0);

    await pageAsOwnerB.goto('/');
    await expect(gridRow(pageAsOwnerB, personas.ownerB.User.Email)).toBeVisible();
    await expect(gridRow(pageAsOwnerB, personas.ownerA.User.Email)).toHaveCount(0);

    // Tenant administration member grids (the page hosts two grids — match any row)
    await pageAsOwnerA.goto('/admin/tenant');
    await expect(pageAsOwnerA.getByRole('row').filter({ hasText: personas.memberA.User.Email })).toBeVisible();
    await expect(pageAsOwnerA.getByRole('row').filter({ hasText: personas.ownerB.User.Email })).toHaveCount(0);

    await pageAsOwnerB.goto('/admin/tenant');
    await expect(pageAsOwnerB.getByRole('row').filter({ hasText: personas.ownerB.User.Email })).toBeVisible();
    await expect(pageAsOwnerB.getByRole('row').filter({ hasText: personas.ownerA.User.Email })).toHaveCount(0);
    await expect(pageAsOwnerB.getByRole('row').filter({ hasText: personas.memberA.User.Email })).toHaveCount(0);
  });
});
