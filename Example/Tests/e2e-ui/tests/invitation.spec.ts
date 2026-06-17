import { test, expect } from '../helpers/fixtures';
import { login } from '../helpers/api';
import { E2E_PASSWORD } from '../helpers/personas';

test.describe('invitations', () => {
  test('owner invites by email and the invitee joins the right tenant', async ({ pageAsOwnerA, page, personas }) => {
    const inviteeEmail = `e2e-invitee-${Date.now()}@example.com`;

    // Owner generates the invite in the tenant admin page
    await pageAsOwnerA.goto('/admin/tenant');
    await pageAsOwnerA.getByRole('textbox', { name: 'Invitee email (optional)' }).fill(inviteeEmail);
    await pageAsOwnerA.getByRole('button', { name: 'Generate Invite' }).click();

    const alertText = await pageAsOwnerA
      .getByRole('alert')
      .filter({ hasText: 'Share this invite link:' })
      .innerText();
    const inviteLink = alertText.replace('Share this invite link:', '').trim();
    expect(inviteLink).toContain('inviteToken=');

    // The invitee opens the link anonymously and is taken to invite-mode registration
    await page.goto(inviteLink);
    await expect(page).toHaveURL(/\/register\?inviteToken=/);
    await expect(page.getByRole('heading', { name: 'Join Tenant' })).toBeVisible();

    // Tenant and email come from the invite (tenant field is locked)
    await expect(page.getByRole('textbox', { name: 'Tenant name' })).toHaveValue(personas.ownerA.Tenant!.Name);
    await expect(page.getByRole('textbox', { name: 'Tenant name' })).toBeDisabled();
    await expect(page.getByRole('textbox', { name: 'Email' })).toHaveValue(inviteeEmail.toLowerCase());

    await page.getByRole('textbox', { name: 'First name' }).fill('Invited');
    await page.getByRole('textbox', { name: 'Surname' }).fill('Member');
    await page.getByRole('textbox', { name: 'Password' }).fill(E2E_PASSWORD);
    await page.getByRole('button', { name: 'Submit Request' }).click();

    // Invite-based registration signs the member in immediately
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('button', { name: 'Logout' })).toBeVisible();

    // The account belongs to the inviting tenant as a member
    const session = await login(inviteeEmail, E2E_PASSWORD);
    expect(session.User.TenantId).toBe(personas.ownerA.User.TenantId);
    expect(session.User.TenantRole).toBe('Member');

    // The invite is single-use: opening the link again shows the invalid/expired error
    const secondVisit = await page.context().newPage();
    await secondVisit.goto(inviteLink);
    await expect(secondVisit.getByRole('alert').filter({ hasText: 'Invite is invalid or expired.' })).toBeVisible();
    await secondVisit.close();
  });

  test('an invalid invite token shows an error on the registration page', async ({ page }) => {
    await page.goto('/accept-invite?token=not-a-real-token');

    await expect(page).toHaveURL(/\/register\?inviteToken=/);
    await expect(page.getByRole('alert').filter({ hasText: 'Invite is invalid or expired.' })).toBeVisible();
  });
});
