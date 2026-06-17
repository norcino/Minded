import { test, expect } from '../helpers/fixtures';
import { uniqueName } from '../helpers/api';
import { clickDialogButton, clickRowAction, dialog, expectToast, fillDialogField, gridRow } from '../helpers/ui';
import type { Page } from '@playwright/test';

async function openUsersGrid(page: Page): Promise<void> {
  await page.goto('/');
  await expect(page.getByRole('grid')).toBeVisible();
}

async function sortUsersNewestFirst(page: Page): Promise<void> {
  const idHeader = page.getByRole('columnheader', { name: 'ID' });
  await idHeader.click();
  await idHeader.click();
}

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}@example.com`.toLowerCase();
}

test.describe('users', () => {
  test('list shows tenant members with their roles and no foreign users', async ({ pageAsOwnerA, personas }) => {
    await openUsersGrid(pageAsOwnerA);

    await expect(gridRow(pageAsOwnerA, personas.ownerA.User.Email)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).toBeVisible();
    // Invited members carry the default application role
    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).toContainText('User');
    // Other tenants' users are not visible
    await expect(gridRow(pageAsOwnerA, personas.ownerB.User.Email)).toHaveCount(0);
  });

  test('create dialog adds a user who receives the default role', async ({ pageAsOwnerA }) => {
    const email = uniqueEmail('e2e-ui-user');

    await openUsersGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add User' }).click();
    await fillDialogField(pageAsOwnerA, 'Name', 'Created');
    await fillDialogField(pageAsOwnerA, 'Surname', 'ByUi');
    await fillDialogField(pageAsOwnerA, 'Email', email);
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expectToast(pageAsOwnerA, 'User created successfully');
    await sortUsersNewestFirst(pageAsOwnerA);
    await expect(gridRow(pageAsOwnerA, email)).toBeVisible();
    // The API assigns the default role automatically on creation
    await expect(gridRow(pageAsOwnerA, email)).toContainText('User');
  });

  test('create dialog rejects missing mandatory fields', async ({ pageAsOwnerA }) => {
    await openUsersGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add User' }).click();
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expect(dialog(pageAsOwnerA).getByText('Name is required', { exact: true })).toBeVisible();
    await expect(dialog(pageAsOwnerA).getByText('Surname is required', { exact: true })).toBeVisible();
    await expect(dialog(pageAsOwnerA).getByText('Email is required', { exact: true })).toBeVisible();
    await expect(dialog(pageAsOwnerA)).toBeVisible();
  });

  test('edit round trip updates user data', async ({ pageAsOwnerA }) => {
    const email = uniqueEmail('e2e-ui-edit');
    const newSurname = uniqueName('Renamed');

    await openUsersGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add User' }).click();
    await fillDialogField(pageAsOwnerA, 'Name', 'Edit');
    await fillDialogField(pageAsOwnerA, 'Surname', 'Target');
    await fillDialogField(pageAsOwnerA, 'Email', email);
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'User created successfully');
    await sortUsersNewestFirst(pageAsOwnerA);
    await clickRowAction(pageAsOwnerA, email, 'Edit');
    await fillDialogField(pageAsOwnerA, 'Surname', newSurname);
    await clickDialogButton(pageAsOwnerA, 'Save');

    await expectToast(pageAsOwnerA, 'User updated successfully');
    await expect(gridRow(pageAsOwnerA, email)).toContainText(newSurname);
  });

  test('delete confirmation: cancel keeps the user, confirm removes them', async ({ pageAsOwnerA }) => {
    const email = uniqueEmail('e2e-ui-delete');

    await openUsersGrid(pageAsOwnerA);
    await pageAsOwnerA.getByRole('button', { name: 'Add User' }).click();
    await fillDialogField(pageAsOwnerA, 'Name', 'Delete');
    await fillDialogField(pageAsOwnerA, 'Surname', 'Target');
    await fillDialogField(pageAsOwnerA, 'Email', email);
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'User created successfully');
    await sortUsersNewestFirst(pageAsOwnerA);
    await clickRowAction(pageAsOwnerA, email, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Cancel');
    await expect(gridRow(pageAsOwnerA, email)).toBeVisible();

    await clickRowAction(pageAsOwnerA, email, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Delete');
    await expectToast(pageAsOwnerA, 'User deleted successfully');
    await expect(gridRow(pageAsOwnerA, email)).toHaveCount(0);
  });
});
