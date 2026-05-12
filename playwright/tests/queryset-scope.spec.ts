import { APIRequestContext, expect, test } from '@playwright/test';
import { unique } from '../helpers/data';

/**
 * Pins the README's "Queryset scope on writes (DRF parity)" contract via TenantNotes:
 * a row-scoping <see cref="TenantNoteTenantFilter"/> reads the X-Tenant header and
 * composes into the load step of every action — so out-of-scope ids surface as 404
 * on GET/{id}, PUT/{id}, PATCH/{id}, DELETE/{id}, and silently drop from DELETE ?ids=.
 *
 * Also covers two adjacent gaps:
 *   - PerformCreateAsync override: the header (not the body) is the canonical TenantId.
 *   - DataAnnotation validation: [StringLength] on TenantNoteDto.Title produces the same
 *     ValidationErrors envelope as serializer-hook 400s.
 */

const TENANT_HEADER = 'X-Tenant';

async function createNote(
  request: APIRequestContext,
  tenant: string,
  overrides: Partial<{ title: string; body: string; tenantId: string }> = {},
): Promise<{ id: number; tenantId: string; title: string; body: string }> {
  const response = await request.post('/api/TenantNotes', {
    headers: { [TENANT_HEADER]: tenant },
    data: {
      title: overrides.title ?? unique('note'),
      body: overrides.body ?? 'sample body',
      // PerformCreateAsync overwrites any body-supplied tenantId with the header — covered in
      // its own test, but we forward whatever the caller passes here so the override site
      // gets exercised on the happy path too.
      ...(overrides.tenantId !== undefined ? { tenantId: overrides.tenantId } : {}),
    },
  });
  expect(response.status()).toBe(201);
  return response.json();
}

test.describe('TenantNotes — queryset scope on writes', () => {
  test('GET /{id} returns 404 when the row is in another tenant', async ({ request }) => {
    const tenantA = unique('tenantA');
    const tenantB = unique('tenantB');
    const note = await createNote(request, tenantA);

    const fromOwner = await request.get(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantA },
    });
    expect(fromOwner.status()).toBe(200);

    const fromIntruder = await request.get(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantB },
    });
    expect(fromIntruder.status()).toBe(404);
  });

  test('PUT /{id} returns 404 when the row is in another tenant — no mutation', async ({ request }) => {
    const tenantA = unique('tenantA');
    const tenantB = unique('tenantB');
    const note = await createNote(request, tenantA, { title: 'original' });

    const intruderPut = await request.put(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantB },
      data: { title: 'attacked', body: 'attacked', tenantId: tenantB },
    });
    expect(intruderPut.status()).toBe(404);

    const stillOriginal = await request.get(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantA },
    });
    expect(stillOriginal.status()).toBe(200);
    expect((await stillOriginal.json()).title).toBe('original');
  });

  test('PATCH /{id} returns 404 when the row is in another tenant — no mutation', async ({ request }) => {
    const tenantA = unique('tenantA');
    const tenantB = unique('tenantB');
    const note = await createNote(request, tenantA, { body: 'kept' });

    const intruderPatch = await request.patch(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantB },
      data: { body: 'tampered' },
    });
    expect(intruderPatch.status()).toBe(404);

    const stillKept = await request.get(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantA },
    });
    expect((await stillKept.json()).body).toBe('kept');
  });

  test('DELETE /{id} returns 404 when the row is in another tenant — row preserved', async ({ request }) => {
    const tenantA = unique('tenantA');
    const tenantB = unique('tenantB');
    const note = await createNote(request, tenantA);

    const intruderDelete = await request.delete(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantB },
    });
    expect(intruderDelete.status()).toBe(404);

    const stillThere = await request.get(`/api/TenantNotes/${note.id}`, {
      headers: { [TENANT_HEADER]: tenantA },
    });
    expect(stillThere.status()).toBe(200);
  });

  test('bulk DELETE silently drops out-of-scope ids (no information leak)', async ({ request }) => {
    const tenantA = unique('tenantA');
    const tenantB = unique('tenantB');

    // Two rows in A, one row in B. B then attempts to bulk-delete all three ids.
    const aOne = await createNote(request, tenantA);
    const aTwo = await createNote(request, tenantA);
    const bOne = await createNote(request, tenantB);

    const bulk = await request.delete(
      `/api/TenantNotes?ids=${aOne.id}&ids=${aTwo.id}&ids=${bOne.id}`,
      { headers: { [TENANT_HEADER]: tenantB } },
    );
    // Bulk delete always returns 204 — there is no per-id error path. Out-of-scope ids are
    // dropped from the SQL DELETE, in-scope ids are removed. Same 404-on-leak posture as
    // single-row DELETE, just collapsed into one statement.
    expect(bulk.status()).toBe(204);

    // A's rows are still alive — B never had visibility on them.
    expect((await request.get(`/api/TenantNotes/${aOne.id}`, {
      headers: { [TENANT_HEADER]: tenantA },
    })).status()).toBe(200);
    expect((await request.get(`/api/TenantNotes/${aTwo.id}`, {
      headers: { [TENANT_HEADER]: tenantA },
    })).status()).toBe(200);

    // B's own row was deleted as part of the same bulk call.
    expect((await request.get(`/api/TenantNotes/${bOne.id}`, {
      headers: { [TENANT_HEADER]: tenantB },
    })).status()).toBe(404);
  });

  test('list returns 200 with an empty envelope when X-Tenant header is missing (defensive empty set)', async ({ request }) => {
    const tenant = unique('tenantA');
    await createNote(request, tenant);

    // No header — TenantNoteTenantFilter returns query.Where(_ => false). The paginator
    // renders the empty source as {count: 0, next: null, previous: null, results: []} —
    // the documented DRF-parity envelope shape (rest_framework/pagination.py:220-226 at
    // encode/django-rest-framework@3.17.1). The information-leak posture still holds at
    // the single-row level: GET /{id} / PUT / PATCH / DELETE for out-of-scope ids return
    // 404 (asserted in the sibling tests above) — DRF has no list-side leak idiom either.
    const headerless = await request.get('/api/TenantNotes');
    expect(headerless.status()).toBe(200);
    const body = await headerless.json();
    expect(body.count).toBe(0);
    expect(body.results).toEqual([]);
    expect(body.next).toBeFalsy();
    expect(body.previous).toBeFalsy();
  });
});

test.describe('TenantNotes — PerformCreateAsync stamps TenantId from the X-Tenant header', () => {
  test('body-supplied tenantId is overwritten by the header', async ({ request }) => {
    const tenantA = unique('tenantA');

    // Caller tries to plant a different tenantId in the body — the controller's
    // PerformCreateAsync override clobbers it with the header value before the serializer
    // creates the row. Match: the README pattern for request-shaped side effects.
    const response = await request.post('/api/TenantNotes', {
      headers: { [TENANT_HEADER]: tenantA },
      data: { tenantId: 'tenant-evil', title: 'override-me', body: '' },
    });
    expect(response.status()).toBe(201);
    expect((await response.json()).tenantId).toBe(tenantA);
  });
});

test.describe('TenantNotes — DataAnnotations on the DTO produce a ValidationErrors envelope', () => {
  test('POST with Title longer than the [StringLength(50)] limit → 400 with envelope', async ({ request }) => {
    const tenant = unique('tenantA');
    const oversized = 'x'.repeat(51);

    const response = await request.post('/api/TenantNotes', {
      headers: { [TENANT_HEADER]: tenant },
      data: { title: oversized, body: 'irrelevant' },
    });
    expect(response.status()).toBe(400);

    // Model-state-driven 400s flow through ApiBehaviorOptions.InvalidModelStateResponseFactory,
    // which the library wires to the same ValidationErrors envelope serializer hooks produce.
    const body = await response.json();
    expect(body.type).toBe('VALIDATION_ERRORS');
    expect(body.statusCode).toBe(400);
    expect(body.error.Title).toEqual(
      expect.arrayContaining([expect.stringMatching(/at most 50 characters/i)]),
    );
  });
});
