import { test as setup } from '@playwright/test';
import { acceptInvite, AuthSession, createInvite, login, registerTenantOwner } from '../helpers/api';
import { E2E_PASSWORD, savePersonas } from '../helpers/personas';

/**
 * Provisions the personas every UI test relies on and writes their storage states.
 * Idempotent: when the database was not reset (reuse mode), existing accounts are
 * logged into instead of re-registered.
 */
setup('provision personas', async () => {
  const globalAdmin = await login('admin@example.com', 'Admin1!');
  const ownerA = await ensureOwner('e2e-owner-a@example.com', 'E2E Tenant A');
  const ownerB = await ensureOwner('e2e-owner-b@example.com', 'E2E Tenant B');
  const memberA = await ensureMember(ownerA, 'e2e-member-a@example.com');

  savePersonas({ globalAdmin, ownerA, ownerB, memberA });
});

async function ensureOwner(email: string, tenantName: string): Promise<AuthSession> {
  try {
    return await login(email, E2E_PASSWORD);
  } catch {
    return registerTenantOwner(email, E2E_PASSWORD, tenantName);
  }
}

async function ensureMember(owner: AuthSession, email: string): Promise<AuthSession> {
  try {
    return await login(email, E2E_PASSWORD);
  } catch {
    const invite = await createInvite(owner, email);
    return acceptInvite(invite.Token, email, E2E_PASSWORD);
  }
}
