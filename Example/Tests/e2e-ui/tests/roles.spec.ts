import { test, expect } from '../helpers/fixtures';
import { api, uniqueName } from '../helpers/api';
import { clickDialogButton, clickRowAction, dialog, expectToast, gridRow } from '../helpers/ui';

/**
 * Role management page (/admin/roles) as tenant owner. Note the application model:
 * roles exist implicitly through their permission assignments, so a role only appears
 * in the list once it has at least one permission.
 */
/** Row anchored on the exact Name cell ('Admin' must not match the 'TenantAdmin' row). */
function roleRow(page: import('@playwright/test').Page, roleName: string) {
  return page.getByRole('row').filter({ has: page.getByRole('gridcell', { name: roleName, exact: true }) });
}

test.describe('role management', () => {
  test('seeded default roles are listed with their permissions', async ({ pageAsOwnerA }) => {
    await pageAsOwnerA.goto('/admin/roles');

    await expect(roleRow(pageAsOwnerA, 'Admin')).toBeVisible();
    await expect(roleRow(pageAsOwnerA, 'TenantAdmin')).toBeVisible();
    await expect(roleRow(pageAsOwnerA, 'User')).toBeVisible();
    await expect(roleRow(pageAsOwnerA, 'TenantAdmin')).toContainText('CanManageRoles');
  });

  test('the Admin role cannot be deleted from the grid', async ({ pageAsOwnerA }) => {
    await pageAsOwnerA.goto('/admin/roles');

    const adminDelete = roleRow(pageAsOwnerA, 'Admin').getByRole('menuitem', { name: 'Delete' })
      .or(roleRow(pageAsOwnerA, 'Admin').getByRole('button', { name: 'Delete' }))
      .first();
    await expect(adminDelete).toBeDisabled();
  });

  test('creating a role succeeds but it appears only once permissions are assigned', async ({ pageAsOwnerA }) => {
    const roleName = uniqueName('UiRole');

    await pageAsOwnerA.goto('/admin/roles');
    await pageAsOwnerA.getByRole('button', { name: 'Add Role' }).click();
    await dialog(pageAsOwnerA).getByRole('textbox', { name: 'Name', exact: true }).fill(roleName);
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'Role created successfully');

    // Implicit-role model: without permissions the role is not listed yet
    await expect(gridRow(pageAsOwnerA, roleName)).toHaveCount(0);
  });

  test('permissions edited through the dialog persist across reloads', async ({ pageAsOwnerA, personas }) => {
    // A dedicated role (created via API with one permission so it is visible in the grid)
    const roleName = uniqueName('UiPermRole');
    await api.put(`/roles/${roleName}/permissions`, ['CanCreateCategory'], personas.ownerA);

    await pageAsOwnerA.goto('/admin/roles');
    await clickRowAction(pageAsOwnerA, roleName, 'Permissions');
    await dialog(pageAsOwnerA).getByRole('checkbox', { name: 'CanCreateTransaction' }).check();
    await clickDialogButton(pageAsOwnerA, 'Save');
    await expectToast(pageAsOwnerA, 'Permissions updated successfully');

    await pageAsOwnerA.reload();
    await expect(gridRow(pageAsOwnerA, roleName)).toContainText('CanCreateTransaction');
  });

  test('deleting a role removes it from the list', async ({ pageAsOwnerA, personas }) => {
    const roleName = uniqueName('UiDeleteRole');
    await api.put(`/roles/${roleName}/permissions`, ['CanCreateCategory'], personas.ownerA);

    await pageAsOwnerA.goto('/admin/roles');
    await expect(gridRow(pageAsOwnerA, roleName)).toBeVisible();

    await clickRowAction(pageAsOwnerA, roleName, 'Delete');
    await clickDialogButton(pageAsOwnerA, 'Delete');
    await expectToast(pageAsOwnerA, 'Role deleted successfully');
    await expect(gridRow(pageAsOwnerA, roleName)).toHaveCount(0);
  });

  test('reset to default restores the seeded role set', async ({ pageAsOwnerA, personas }) => {
    // Drift: an extra custom role with a permission
    const roleName = uniqueName('UiDriftRole');
    await api.put(`/roles/${roleName}/permissions`, ['CanCreateCategory'], personas.ownerA);

    await pageAsOwnerA.goto('/admin/roles');
    await expect(gridRow(pageAsOwnerA, roleName)).toBeVisible();

    await pageAsOwnerA.getByRole('button', { name: 'Reset to Default' }).click();
    await expectToast(pageAsOwnerA, 'Roles reset to default successfully');

    await expect(gridRow(pageAsOwnerA, roleName)).toHaveCount(0);
    await expect(gridRow(pageAsOwnerA, 'TenantAdmin')).toBeVisible();
  });

  test('members do not see role administration in the navigation', async ({ pageAsMemberA }) => {
    await pageAsMemberA.goto('/');

    await expect(pageAsMemberA.getByRole('button', { name: 'Roles', exact: true })).toHaveCount(0);
    await expect(pageAsMemberA.getByRole('button', { name: 'Tenant Admin' })).toHaveCount(0);
  });
});
