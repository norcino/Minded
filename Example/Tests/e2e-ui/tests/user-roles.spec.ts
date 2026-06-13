import { test, expect } from '../helpers/fixtures';
import { api } from '../helpers/api';
import { clickDialogButton, clickRowAction, dialog, expectToast, gridRow } from '../helpers/ui';

/**
 * User role assignment page (/admin/user-roles) as tenant owner.
 * The shared memberA persona is always restored to its default ['User'] role set,
 * so other suites can rely on the member being a plain member.
 */
test.describe('user role assignment', () => {
  test.afterEach(async ({ personas }) => {
    await api.put(`/users/${personas.memberA.User.Id}/roles`, ['User'], personas.ownerA);
  });

  test('members are listed with their role chips', async ({ pageAsOwnerA, personas }) => {
    await pageAsOwnerA.goto('/admin/user-roles');

    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).toBeVisible();
    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).toContainText('User');
  });

  test('assigning an extra role persists across reloads', async ({ pageAsOwnerA, personas }) => {
    await pageAsOwnerA.goto('/admin/user-roles');

    await clickRowAction(pageAsOwnerA, personas.memberA.User.Email, 'Edit Roles');
    await dialog(pageAsOwnerA).getByRole('checkbox', { name: 'TenantAdmin' }).check();
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'Roles updated for');

    await pageAsOwnerA.reload();
    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).toContainText('TenantAdmin');
    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).toContainText('User');
  });

  test('revoking a role removes its chip', async ({ pageAsOwnerA, personas }) => {
    // Start from User + TenantAdmin
    await api.put(`/users/${personas.memberA.User.Id}/roles`, ['User', 'TenantAdmin'], personas.ownerA);

    await pageAsOwnerA.goto('/admin/user-roles');
    await clickRowAction(pageAsOwnerA, personas.memberA.User.Email, 'Edit Roles');
    await dialog(pageAsOwnerA).getByRole('checkbox', { name: 'TenantAdmin' }).uncheck();
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'Roles updated for');

    await pageAsOwnerA.reload();
    await expect(gridRow(pageAsOwnerA, personas.memberA.User.Email)).not.toContainText('TenantAdmin');
  });

  test('granting the TenantAdmin role gives the member admin navigation', async ({ pageAsOwnerA, pageAsMemberA, personas }) => {
    // Baseline: a plain member has no admin navigation
    await pageAsMemberA.goto('/');
    await expect(pageAsMemberA.getByRole('button', { name: 'Roles', exact: true })).toHaveCount(0);

    // Owner grants the application TenantAdmin role through the UI
    await pageAsOwnerA.goto('/admin/user-roles');
    await clickRowAction(pageAsOwnerA, personas.memberA.User.Email, 'Edit Roles');
    await dialog(pageAsOwnerA).getByRole('checkbox', { name: 'TenantAdmin' }).check();
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'Roles updated for');

    // The member's session reflects the new role after a reload (/me refresh)
    await pageAsMemberA.reload();
    await expect(pageAsMemberA.getByRole('button', { name: 'Roles', exact: true })).toBeVisible();
  });
});
