import { test, expect } from '../helpers/fixtures';
import { createCategory, createTransaction, uniqueName } from '../helpers/api';
import { clickDialogButton, clickRowAction, dialog, expectToast, fillDialogField, gridRow } from '../helpers/ui';
import type { Page } from '@playwright/test';

async function openTransactionsGrid(page: Page): Promise<void> {
  await page.goto('/transactions');
  await expect(page.getByRole('grid')).toBeVisible();

  // Newest-first so rows created by the current test are on the visible page
  await page.getByRole('columnheader', { name: 'ID' }).click();
  await page.getByRole('columnheader', { name: 'ID' }).click();
}

test.describe('transactions', () => {
  test('grid lists transactions with their category', async ({ pageAsOwnerA, personas }) => {
    const categoryName = uniqueName('TxCat');
    const description = uniqueName('TxList');
    const category = await createCategory(personas.ownerA, categoryName);
    await createTransaction(personas.ownerA, category.Id, description);

    await openTransactionsGrid(pageAsOwnerA);
    await expect(gridRow(pageAsOwnerA, description)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, description)).toContainText(categoryName);
  });

  test('create dialog adds a transaction showing category and todays date', async ({ pageAsOwnerA, personas }) => {
    const categoryName = uniqueName('TxNewCat');
    const description = uniqueName('TxNew');
    await createCategory(personas.ownerA, categoryName);

    await openTransactionsGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add Transaction' }).click();
    await fillDialogField(pageAsOwnerA, 'Description', description);
    await dialog(pageAsOwnerA).getByRole('combobox', { name: 'Category' }).click();
    await pageAsOwnerA.getByRole('option', { name: categoryName }).click();
    await dialog(pageAsOwnerA).getByRole('spinbutton', { name: 'Credit' }).fill('123.45');
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expectToast(pageAsOwnerA, 'Transaction created successfully');

    // Date handling: the new row shows today's date in the grid's display format
    const expectedDate = new Date().toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
    await expect(gridRow(pageAsOwnerA, description)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, description)).toContainText(expectedDate);
    await expect(gridRow(pageAsOwnerA, description)).toContainText(categoryName);
  });

  test('create dialog rejects a missing description', async ({ pageAsOwnerA }) => {
    await openTransactionsGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add Transaction' }).click();
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expect(dialog(pageAsOwnerA).getByText('Description is required')).toBeVisible();
    await expect(dialog(pageAsOwnerA)).toBeVisible();
  });

  test('edit round trip updates the description', async ({ pageAsOwnerA, personas }) => {
    const category = await createCategory(personas.ownerA, uniqueName('TxEditCat'));
    const original = uniqueName('TxEdit');
    const updated = uniqueName('TxUpdated');
    await createTransaction(personas.ownerA, category.Id, original);

    await openTransactionsGrid(pageAsOwnerA);
    await clickRowAction(pageAsOwnerA, original, 'Edit');
    await fillDialogField(pageAsOwnerA, 'Description', updated);
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expectToast(pageAsOwnerA, 'Transaction updated successfully');
    await expect(gridRow(pageAsOwnerA, updated)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, original)).toHaveCount(0);
  });

  test('delete confirmation: cancel keeps the row, confirm removes it', async ({ pageAsOwnerA, personas }) => {
    const category = await createCategory(personas.ownerA, uniqueName('TxDelCat'));
    const description = uniqueName('TxDelete');
    await createTransaction(personas.ownerA, category.Id, description);

    await openTransactionsGrid(pageAsOwnerA);
    await clickRowAction(pageAsOwnerA, description, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Cancel');
    await expect(gridRow(pageAsOwnerA, description)).toBeVisible();

    await clickRowAction(pageAsOwnerA, description, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Delete');
    await expectToast(pageAsOwnerA, 'Transaction deleted successfully');
    await expect(gridRow(pageAsOwnerA, description)).toHaveCount(0);
  });
});
