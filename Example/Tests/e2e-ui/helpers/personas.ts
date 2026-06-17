import * as fs from 'node:fs';
import * as path from 'node:path';
import { AuthSession } from './api';

/**
 * Persona registry: identities provisioned once per run by tests/personas.setup.ts.
 * Storage-state files give browser contexts a ready, authenticated session exactly the
 * way the frontend stores it (accessToken + camelCase currentUser in localStorage).
 */
export const E2E_PASSWORD = 'Passw0rd!';

const AUTH_DIR = path.join(__dirname, '..', '.auth');
const REGISTRY_FILE = path.join(AUTH_DIR, 'personas.json');

export type PersonaName = 'globalAdmin' | 'ownerA' | 'ownerB' | 'memberA';

export interface PersonaRegistry {
  globalAdmin: AuthSession;
  ownerA: AuthSession;
  ownerB: AuthSession;
  memberA: AuthSession;
}

export function storageStatePath(persona: PersonaName): string {
  return path.join(AUTH_DIR, `${persona}.json`);
}

export function savePersonas(registry: PersonaRegistry): void {
  fs.mkdirSync(AUTH_DIR, { recursive: true });
  fs.writeFileSync(REGISTRY_FILE, JSON.stringify(registry, null, 2));

  for (const [name, session] of Object.entries(registry) as [PersonaName, AuthSession][]) {
    fs.writeFileSync(storageStatePath(name), JSON.stringify(buildStorageState(session), null, 2));
  }
}

export function loadPersonas(): PersonaRegistry {
  if (!fs.existsSync(REGISTRY_FILE)) {
    throw new Error('Persona registry not found - the "setup" project must run before the tests.');
  }
  return JSON.parse(fs.readFileSync(REGISTRY_FILE, 'utf8')) as PersonaRegistry;
}

/** Builds a Playwright storage state replicating what the frontend writes after login. */
function buildStorageState(session: AuthSession): unknown {
  // The frontend stores the user in camelCase (its axios layer converts API PascalCase)
  const currentUser = {
    id: session.User.Id,
    tenantId: session.User.TenantId,
    name: session.User.Name,
    surname: session.User.Surname,
    email: session.User.Email,
    tenantRole: session.User.TenantRole,
    isGlobalAdmin: session.User.IsGlobalAdmin,
  };

  const localStorage = [
    { name: 'accessToken', value: session.AccessToken },
    { name: 'currentUser', value: JSON.stringify(currentUser) },
  ];

  if (session.Tenant?.Name) {
    localStorage.push({ name: 'tenantName', value: session.Tenant.Name });
  }

  return {
    cookies: [],
    origins: [
      {
        origin: 'http://localhost:3000',
        localStorage,
      },
    ],
  };
}
