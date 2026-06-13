import { Client } from 'pg';

/**
 * Direct read access to the UI E2E database for state that has no API surface
 * (password-reset tokens, invite tokens are normally delivered by email).
 * Use the API helpers for everything that CAN be arranged through the API.
 */
export async function queryDatabase<T extends Record<string, unknown>>(
  sql: string,
  params: unknown[] = [],
): Promise<T[]> {
  const client = new Client({
    host: 'localhost',
    port: 5433,
    database: 'mindedexample_e2e',
    user: 'minded',
    password: 'minded',
  });

  await client.connect();
  try {
    const result = await client.query(sql, params);
    return result.rows as T[];
  } finally {
    await client.end();
  }
}

/** Returns the most recent password-reset token issued for the given email, if any. */
export async function getLatestPasswordResetToken(email: string): Promise<string | undefined> {
  const rows = await queryDatabase<{ Token: string }>(
    `SELECT t."Token"
       FROM dbo."PasswordResetTokens" t
       JOIN dbo."Users" u ON u."Id" = t."UserId"
      WHERE lower(u."Email") = lower($1)
      ORDER BY t."Id" DESC
      LIMIT 1`,
    [email],
  );
  return rows[0]?.Token;
}

/** Returns the most recent invite token created for the given tenant, if any. */
export async function getLatestInviteToken(tenantId: number): Promise<string | undefined> {
  const rows = await queryDatabase<{ Token: string }>(
    `SELECT i."Token"
       FROM dbo."TenantInvites" i
      WHERE i."TenantId" = $1
      ORDER BY i."Id" DESC
      LIMIT 1`,
    [tenantId],
  );
  return rows[0]?.Token;
}
