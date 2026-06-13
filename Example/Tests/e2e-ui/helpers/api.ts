import * as http from 'node:http';

/**
 * Thin authenticated client for the Example API, used to arrange test data through real
 * endpoints (preferred over direct database writes — survives schema refactors).
 * Implemented with node:http rather than fetch: the API listens on port 6000, which is on
 * the fetch specification's "bad ports" list (X11), so undici refuses to connect to it.
 * Note: the API serializes JSON in PascalCase; response properties are used as-is.
 */
const API_BASE = 'http://localhost:6000/api';

export interface ApiResponse {
  status: number;
  ok: boolean;
  body: string;
  json<T>(): T;
}

function request(method: string, url: string, headers: Record<string, string>, body?: string): Promise<ApiResponse> {
  return new Promise((resolve, reject) => {
    const req = http.request(url, { method, headers }, res => {
      const chunks: Buffer[] = [];
      res.on('data', chunk => chunks.push(chunk));
      res.on('end', () => {
        const text = Buffer.concat(chunks).toString('utf8');
        resolve({
          status: res.statusCode ?? 0,
          ok: (res.statusCode ?? 0) >= 200 && (res.statusCode ?? 0) < 300,
          body: text,
          json: <T>() => JSON.parse(text) as T,
        });
      });
    });
    req.on('error', reject);
    if (body !== undefined) {
      req.write(body);
    }
    req.end();
  });
}

export interface AuthSession {
  AccessToken: string;
  User: {
    Id: number;
    TenantId: number | null;
    Name: string;
    Surname: string;
    Email: string;
    TenantRole: string;
    IsGlobalAdmin: boolean;
  };
  Tenant?: { Id: number; Name: string };
}

async function send(method: string, path: string, body?: unknown, session?: AuthSession): Promise<ApiResponse> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (session) {
    headers.Authorization = `Bearer ${session.AccessToken}`;
  }

  return request(method, `${API_BASE}${path}`, headers, body === undefined ? undefined : JSON.stringify(body));
}

async function sendExpectingSuccess<T>(method: string, path: string, body?: unknown, session?: AuthSession): Promise<T> {
  const response = await send(method, path, body, session);
  if (!response.ok) {
    throw new Error(`${method} ${path} failed with ${response.status}: ${response.body}`);
  }
  return response.json<T>();
}

export const api = {
  get: <T>(path: string, session: AuthSession) => sendExpectingSuccess<T>('GET', path, undefined, session),
  post: <T>(path: string, body: unknown, session?: AuthSession) => sendExpectingSuccess<T>('POST', path, body, session),
  put: <T>(path: string, body: unknown, session: AuthSession) => sendExpectingSuccess<T>('PUT', path, body, session),
  raw: send,
};

/** Logs in through the API and returns the authenticated session. */
export async function login(email: string, password: string): Promise<AuthSession> {
  return sendExpectingSuccess<AuthSession>('POST', '/auth/login', { Email: email, Password: password });
}

/** Registers a brand-new tenant (create-tenant mode) and returns the authenticated owner. */
export async function registerTenantOwner(
  email: string,
  password: string,
  tenantName: string,
): Promise<AuthSession> {
  return sendExpectingSuccess<AuthSession>('POST', '/auth/register', {
    Name: 'E2E',
    Surname: 'Owner',
    Email: email,
    Password: password,
    TenantName: tenantName,
  });
}

/** Creates a category owned by the session user and returns it. */
export async function createCategory(session: AuthSession, name: string): Promise<{ Id: number; Name: string }> {
  return api.post('/category', {
    Name: name,
    Description: `${name} description`,
    Active: true,
    UserId: session.User.Id,
  }, session);
}

/** Creates a transaction in the given category and returns it. */
export async function createTransaction(
  session: AuthSession,
  categoryId: number,
  description: string,
): Promise<{ Id: number; Description: string }> {
  return api.post('/transaction', {
    Description: description,
    Credit: 42.5,
    Debit: 0,
    Recorded: new Date().toISOString(),
    CategoryId: categoryId,
    UserId: session.User.Id,
  }, session);
}

/** Creates a tenant invite as the given (owner/admin) session and returns its token and code. */
export async function createInvite(
  session: AuthSession,
  inviteeEmail: string,
): Promise<{ Token: string; Code: string }> {
  return api.post('/tenant-admin/invites', { Email: inviteeEmail }, session);
}

/** Accepts an invite, creating a member account, and returns the authenticated member. */
export async function acceptInvite(
  tokenOrCode: string,
  email: string,
  password: string,
): Promise<AuthSession> {
  return sendExpectingSuccess<AuthSession>('POST', '/auth/accept-invite', {
    CodeOrToken: tokenOrCode,
    Email: email,
    Name: 'E2E',
    Surname: 'Member',
    Password: password,
  });
}

/** Unique-enough suffix for test data names (single worker, per-run database). */
export function uniqueName(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}`;
}
