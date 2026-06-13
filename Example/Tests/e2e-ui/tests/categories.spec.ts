import { test, expect } from '../helpers/fixtures';
import { createCategory, uniqueName } from '../helpers/api';
import { clickDialogButton, clickRowAction, dialog, expectToast, fillDialogField, gridRow } from '../helpers/ui';
import type { Page } from '@playwright/test';

async function openCategoriesGrid(page: Page): Promise<void> {
  await page.goto('/categories');
  await page.getByRole('button', { name: 'Grid view' }).click();
  await expect(page.getByRole('grid')).toBeVisible();

  // Sort newest-first so rows created by the current test are on the visible page,
  // regardless of how much data previous tests accumulated in the shared database
  await page.getByRole('columnheader', { name: 'ID' }).click();
  await page.getByRole('columnheader', { name: 'ID' }).click();
}

test.describe('categories', () => {
  test('grid lists categories created through the API', async ({ pageAsOwnerA, personas }) => {
    const names = [uniqueName('CatList1'), uniqueName('CatList2'), uniqueName('CatList3')];
    for (const name of names) {
      await createCategory(personas.ownerA, name);
    }

    await openCategoriesGrid(pageAsOwnerA);
    for (const name of names) {
      await expect(gridRow(pageAsOwnerA, name)).toBeVisible();
    }
  });

  test('create dialog adds a category and shows a success toast', async ({ pageAsOwnerA }) => {
    const name = uniqueName('CatNew');

    await openCategoriesGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add Category' }).click();
    await fillDialogField(pageAsOwnerA, 'Name', name);
    await fillDialogField(pageAsOwnerA, 'Description', `${name} description`);
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expectToast(pageAsOwnerA, 'Category created successfully');
    await expect(gridRow(pageAsOwnerA, name)).toBeVisible();
  });

  test('create dialog rejects an empty name', async ({ pageAsOwnerA }) => {
    await openCategoriesGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add Category' }).click();
    await fillDialogField(pageAsOwnerA, 'Description', 'No name provided');
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expect(dialog(pageAsOwnerA).getByText('Name is required')).toBeVisible();
    await expect(dialog(pageAsOwnerA)).toBeVisible();
  });

  test('edit round trip renames a category', async ({ pageAsOwnerA, personas }) => {
    const original = uniqueName('CatEdit');
    const renamed = uniqueName('CatRenamed');
    await createCategory(personas.ownerA, original);

    await openCategoriesGrid(pageAsOwnerA);
    await clickRowAction(pageAsOwnerA, original, 'Edit');
    await fillDialogField(pageAsOwnerA, 'Name', renamed);
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expectToast(pageAsOwnerA, 'Category updated successfully');
    // Assert on the Name cell precisely: the description cell still contains the old name
    await expect(pageAsOwnerA.getByRole('gridcell', { name: renamed, exact: true })).toBeVisible();
    await expect(pageAsOwnerA.getByRole('gridcell', { name: original, exact: true })).toHaveCount(0);
  });

  test('delete confirmation: cancel keeps the row, confirm removes it', async ({ pageAsOwnerA, personas }) => {
    const name = uniqueName('CatDelete');
    await createCategory(personas.ownerA, name);

    await openCategoriesGrid(pageAsOwnerA);
    await clickRowAction(pageAsOwnerA, name, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Cancel');
    await expect(gridRow(pageAsOwnerA, name)).toBeVisible();

    await clickRowAction(pageAsOwnerA, name, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Delete');
    await expectToast(pageAsOwnerA, 'Category deleted successfully');
    await expect(gridRow(pageAsOwnerA, name)).toHaveCount(0);
  });

  test('a child category appears under its parent in the tree view', async ({ pageAsOwnerA, personas }) => {
    const parentName = uniqueName('CatParent');
    const childName = uniqueName('CatChild');
    await createCategory(personas.ownerA, parentName);

    await pageAsOwnerA.goto('/categories');
    await pageAsOwnerA.getByRole('button', { name: 'Add Root Category' }).click();
    await fillDialogField(pageAsOwnerA, 'Name', childName);
    await fillDialogField(pageAsOwnerA, 'Description', `${childName} description`);
    await dialog(pageAsOwnerA).getByRole('combobox', { name: 'Parent Category' }).click();
    await pageAsOwnerA.getByRole('option', { name: parentName }).click();
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'Category created successfully');

    // Both parent and child are visible in the (expanded) tree. Locators are scoped to
    // tree items: the live LogConsole at the bottom also prints the category names.
    await pageAsOwnerA.getByRole('button', { name: 'Expand all categories' }).click();
    await expect(pageAsOwnerA.getByRole('treeitem').filter({ hasText: parentName }).first()).toBeVisible();
    await expect(pageAsOwnerA.getByRole('treeitem').filter({ hasText: childName }).first()).toBeVisible();
  });

  test('grid supports paging and sorting over a full data set', async ({ pageAsOwnerA, personas }) => {
    // 12 rows whose names sort LAST and, within "zzz" batches, newest-first when
    // descending — deterministic regardless of data accumulated by previous tests/runs
    const prefix = `zzz-${Date.now()}`;
    for (let i = 1; i <= 12; i++) {
      await createCategory(personas.ownerA, `${prefix}-${String(i).padStart(2, '0')}`);
    }

    await openCategoriesGrid(pageAsOwnerA);

    // Sort descending by name: first page holds rows 12..03 of this batch
    await pageAsOwnerA.getByRole('columnheader', { name: 'Name' }).click();
    await pageAsOwnerA.getByRole('columnheader', { name: 'Name' }).click();
    await expect(gridRow(pageAsOwnerA, `${prefix}-12`)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, `${prefix}-02`)).toHaveCount(0);

    // Second page shows the remaining rows
    await pageAsOwnerA.getByRole('button', { name: 'Go to next page' }).click();
    await expect(gridRow(pageAsOwnerA, `${prefix}-02`)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, `${prefix}-12`)).toHaveCount(0);
  });
});
