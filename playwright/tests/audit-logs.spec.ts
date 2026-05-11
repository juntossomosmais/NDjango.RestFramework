import { expect, test } from '@playwright/test';
import { unique } from '../helpers/data';

/**
 * AuditLogs is wired as an append-only resource via ActionOptions:
 *   AllowPatch     = false
 *   AllowPut       = false
 *   AllowDelete    = false
 *   AllowBulkDelete = false
 *
 * GET (list + item) and POST are the only allowed verbs. Every other call returns
 * 405 Method Not Allowed inline — the endpoint stays listed in OpenAPI (asserted in
 * openapi.spec.ts) but the runtime guard short-circuits before the action body.
 */
test.describe('AuditLogs — HTTP method allowlist', () => {
  let id = 0;

  test.beforeAll(async ({ request }) => {
    const response = await request.post('/api/AuditLogs', {
      data: { action: 'CREATE', entityName: 'Tag', detail: unique('seed') },
    });
    expect(response.status()).toBe(201);
    id = (await response.json()).id;
  });

  test('GET list and GET single succeed', async ({ request }) => {
    const list = await request.get('/api/AuditLogs');
    expect(list.status()).toBe(200);
    expect((await list.json()).count).toBeGreaterThanOrEqual(1);

    const single = await request.get(`/api/AuditLogs/${id}`);
    expect(single.status()).toBe(200);
  });

  test('POST succeeds (append-only is still write-once-allowed)', async ({ request }) => {
    const response = await request.post('/api/AuditLogs', {
      data: { action: 'UPDATE', entityName: 'Restaurant', detail: 'ok' },
    });
    expect(response.status()).toBe(201);
  });

  test('PATCH returns 405', async ({ request }) => {
    const response = await request.patch(`/api/AuditLogs/${id}`, { data: { detail: 'edited' } });
    expect(response.status()).toBe(405);
  });

  test('PUT returns 405', async ({ request }) => {
    const response = await request.put(`/api/AuditLogs/${id}`, {
      data: { action: 'DELETE', entityName: 'X', detail: null },
    });
    expect(response.status()).toBe(405);
  });

  test('single DELETE returns 405', async ({ request }) => {
    const response = await request.delete(`/api/AuditLogs/${id}`);
    expect(response.status()).toBe(405);
  });

  test('bulk DELETE returns 405', async ({ request }) => {
    const response = await request.delete(`/api/AuditLogs?ids=${id}`);
    expect(response.status()).toBe(405);
  });
});
