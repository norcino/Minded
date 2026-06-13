import { test as base, Page } from '@playwright/test';
import { loadPersonas, PersonaRegistry, storageStatePath } from './personas';

/**
 * Test fixtures exposing pre-authenticated pages per persona (storage states created by
 * the "setup" project) plus the persona registry for API-side arrangement.
 * The default `page` fixture remains anonymous.
 */
interface PersonaFixtures {
  /** Identities and API sessions of the provisioned personas. */
  personas: PersonaRegistry;
  /** Page authenticated as the global administrator. */
  pageAsGlobalAdmin: Page;
  /** Page authenticated as the owner of tenant A. */
  pageAsOwnerA: Page;
  /** Page authenticated as the owner of tenant B. */
  pageAsOwnerB: Page;
  /** Page authenticated as a plain member of tenant A. */
  pageAsMemberA: Page;
}

function personaPage(persona: Parameters<typeof storageStatePath>[0]) {
  return async (
    { browser }: { browser: import('@playwright/test').Browser },
    use: (page: Page) => Promise<void>,
  ) => {
    const context = await browser.newContext({ storageState: storageStatePath(persona) });
    const page = await context.newPage();
    await use(page);
    await context.close();
  };
}

export const test = base.extend<PersonaFixtures>({
  personas: async ({}, use) => {
    await use(loadPersonas());
  },
  pageAsGlobalAdmin: personaPage('globalAdmin'),
  pageAsOwnerA: personaPage('ownerA'),
  pageAsOwnerB: personaPage('ownerB'),
  pageAsMemberA: personaPage('memberA'),
});

export { expect } from '@playwright/test';
