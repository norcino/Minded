import { test, expect } from '../helpers/fixtures';
import { acceptInvite, api, AuthSession, createInvite } from '../helpers/api';
import { E2E_PASSWORD } from '../helpers/personas';
import { expectToast } from '../helpers/ui';
import type { Page } from '@playwright/test';

/** Member row in whichever grid contains it (the page hosts two grids). */
function memberRow(page: Page, email: string) {
  return page.getByRole('row').filter({ hasText: email });
}

/**
 * Sorts the members grid (the first grid on the page) by Email descending.
 * Freshly created e2e-ta-* accounts get the highest ids and land at the BOTTOM of the
 * default order, where the fixed-height DataGrid virtualizes them out of the DOM once
 * the tenant has accumulated enough members — sorting puts them back in the viewport.
 */
async function sortMembersByEmailDesc(page: Page): Promise<void> {
  const emailHeader = page.getByRole('grid').first().getByRole('columnheader', { name: 'Email' });
  await emailHeader.click();
  await emailHeader.click();
}

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}@example.com`.toLowerCase();
}

/** Creates a disposable member of tenant A through the invite API. */
async function createDisposableMember(owner: AuthSession): Promise<AuthSession> {
  const email = uniqueEmail('e2e-ta-member');
  const invite = await createInvite(owner, email);
  return acceptInvite(invite.Token, email, E2E_PASSWORD);
}

/** Registers a pending join request for the given tenant and returns the joiner email. */
async function registerJoinRequest(tenantName: string): Promise<string> {
  const email = uniqueEmail('e2e-ta-joiner');
  const response = await api.raw('POST', '/auth/register', {
    Name: 'Join',
    Surname: 'Requester',
    Email: email,
    Password: E2E_PASSWORD,
    TenantName: tenantName,
    Mode: 'join-tenant',
  });
  if (!response.ok) {
    throw new Error(`join-tenant registration failed: ${response.status}`);
  }
  return email;
}

test.describe('tenant administration', () => {
  test('members are listed and own-row controls are locked', async ({ pageAsOwnerA, personas }) => {
    await pageAsOwnerA.goto('/admin/tenant');

    await expect(memberRow(pageAsOwnerA, personas.ownerA.User.Email)).toBeVisible();
    await expect(memberRow(pageAsOwnerA, personas.memberA.User.Email)).toBeVisible();

    // The owner cannot change or remove their own membership
    const ownRow = memberRow(pageAsOwnerA, personas.ownerA.User.Email);
    await expect(ownRow.getByRole('combobox')).toHaveAttribute('aria-disabled', 'true');
    await expect(ownRow.getByRole('button', { name: 'Remove' })).toBeDisabled();
  });

  test('changing a member tenant role persists', async ({ pageAsOwnerA, personas }) => {
    const member = await createDisposableMember(personas.ownerA);

    await pageAsOwnerA.goto('/admin/tenant');
    await sortMembersByEmailDesc(pageAsOwnerA);
    await memberRow(pageAsOwnerA, member.User.Email).getByRole('combobox').click();
    await pageAsOwnerA.getByRole('option', { name: 'Admin' }).click();
    await expectToast(pageAsOwnerA, 'Role updated.');

    await pageAsOwnerA.reload();
    await sortMembersByEmailDesc(pageAsOwnerA);
    await expect(memberRow(pageAsOwnerA, member.User.Email).getByRole('combobox')).toHaveText('Admin');
  });

  test('removing a member revokes their access entirely', async ({ pageAsOwnerA, personas }) => {
    const member = await createDisposableMember(personas.ownerA);
    const memberEmail = member.User.Email;

    await pageAsOwnerA.goto('/admin/tenant');
    await sortMembersByEmailDesc(pageAsOwnerA);
    await memberRow(pageAsOwnerA, memberEmail).getByRole('button', { name: 'Remove' }).click();
    await expectToast(pageAsOwnerA, 'User removed.');
    await expect(memberRow(pageAsOwnerA, memberEmail)).toHaveCount(0);

    // The removed account can no longer log in
    const login = await api.raw('POST', '/auth/login', { Email: memberEmail, Password: E2E_PASSWORD });
    expect(login.status).toBe(401);
  });

  test('approving a join request creates a working account in the tenant', async ({ pageAsOwnerA, personas }) => {
    const joinerEmail = await registerJoinRequest(personas.ownerA.Tenant!.Name);

    await pageAsOwnerA.goto('/admin/tenant');
    await sortMembersByEmailDesc(pageAsOwnerA);
    await memberRow(pageAsOwnerA, joinerEmail).getByRole('button', { name: 'Approve' }).click();
    await expectToast(pageAsOwnerA, 'Join request approved.');

    // The joiner moved from the pending grid into the members grid
    await expect(memberRow(pageAsOwnerA, joinerEmail).getByRole('button', { name: 'Remove' })).toBeVisible();

    const login = await api.raw('POST', '/auth/login', { Email: joinerEmail, Password: E2E_PASSWORD });
    expect(login.ok).toBe(true);
    expect(login.json<AuthSession>().User.TenantId).toBe(personas.ownerA.User.TenantId);
  });

  test('rejecting a join request keeps the applicant blocked', async ({ pageAsOwnerA, personas }) => {
    const joinerEmail = await registerJoinRequest(personas.ownerA.Tenant!.Name);

    await pageAsOwnerA.goto('/admin/tenant');
    await memberRow(pageAsOwnerA, joinerEmail).getByRole('button', { name: 'Reject' }).click();
    await expectToast(pageAsOwnerA, 'Join request rejected.');
    await expect(memberRow(pageAsOwnerA, joinerEmail)).toHaveCount(0);

    const login = await api.raw('POST', '/auth/login', { Email: joinerEmail, Password: E2E_PASSWORD });
    expect(login.status).toBe(401);
  });

  test('a plain member cannot load tenant administration data', async ({ pageAsMemberA }) => {
    // The route renders, but the management API rejects the member (403) and the page surfaces it.
    // Users and join requests load in parallel into a shared error state — either message proves it.
    await pageAsMemberA.goto('/admin/tenant');
    await expect(
      pageAsMemberA.getByRole('alert').filter({ hasText: /Failed to load (tenant users|pending join requests)\./ }),
    ).toBeVisible();
  });
});
