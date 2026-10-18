import { expect, APIRequestContext } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const env = Object.fromEntries(
  readFileSync(resolve('../.env'), 'utf8')
    .split('\n')
    .filter((line) => line && !line.startsWith('#') && line.includes('='))
    .map((line) => {
      const i = line.indexOf('=');
      return [line.slice(0, i), line.slice(i + 1)];
    }),
);
export const adminCredentials = { email: env['ADMIN_EMAIL'], password: env['ADMIN_PASSWORD'] };

export async function adminWrite(
  request: APIRequestContext,
  origin: string,
  path: string,
  data: unknown = {},
  method = 'POST',
) {
  const csrf = await (await request.get(origin + '/api/auth/csrf')).json();
  return request.fetch(origin + path, { method, data, headers: { 'X-CSRF-TOKEN': csrf.token } });
}

export async function loginAdmin(request: APIRequestContext, origin: string) {
  const response = await adminWrite(request, origin, '/api/auth/login', adminCredentials);
  expect(response.status()).toBe(200);
}
