import { test, expect } from '@playwright/test';
import { api, createCategory, login, uniqueName } from '../helpers/api';
import { getLatestPasswordResetToken } from '../helpers/db';

/**
 * Proves the test infrastructure itself: data can be arranged through the API helpers,
 * and state without an API surface (password-reset tokens) can be read from the database.
 */
test.describe('infrastructure', () => {
  test('api helper arranges data through real endpoints', async () => {
    const session = await login('admin-tenant1@example.com', 'Admin1!');
    const name = uniqueName('Category');

    const created = await createCategory(session, name);
    expect(created.Id).toBeGreaterThan(0);

    const categories = await api.get<Array<{ Id: number; Name: string }>>('/category', session);
    expect(categories.some(c => c.Id === created.Id && c.Name === name)).toBe(true);
  });

  test('db helper reads state that has no API surface', async () => {
    const email = 'admin-tenant1@example.com';
    const response = await api.raw('POST', '/auth/forgot-password', { Email: email });
    expect(response.ok).toBe(true);

    const token = await getLatestPasswordResetToken(email);
    expect(token).toBeTruthy();
  });
});
