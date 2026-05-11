import { expect, test } from '@playwright/test';
import { unique } from '../helpers/data';

/**
 * Drives the per-field Validate{X}Async hooks on TagSerializer:
 *   - ValidateNameAsync trims, requires non-empty, enforces uniqueness.
 *   - ValidateSlugAsync normalizes to lowercase kebab-case, enforces uniqueness.
 *
 * Tags are flag-gated like every other resource — full CRUD works.
 */
test.describe('Tags — per-field validation hooks', () => {
  test('happy path: ValidateSlugAsync normalizes the persisted slug', async ({ request }) => {
    // The slug input is unique-per-run so consecutive playwright runs don't trip the
    // uniqueness check on a slug that always normalizes to the same value.
    const name = unique('Spicy Pepper');
    const rawSlug = `Spicy   ${unique('Pepper')}!!`;
    const expectedSlug = rawSlug
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');

    const create = await request.post('/api/Tags', {
      data: { name, slug: rawSlug },
    });
    expect(create.status()).toBe(201);
    const tag = await create.json();
    // The hook returned the normalized slug, and that's what got persisted.
    expect(tag.slug).toBe(expectedSlug);
    // Name is trimmed, not lowercased — only Slug normalizes.
    expect(tag.name).toBe(name);
  });

  test('ValidateNameAsync rejects empty name with field-level error', async ({ request }) => {
    const response = await request.post('/api/Tags', {
      data: { name: '   ', slug: unique('slug') },
    });
    expect(response.status()).toBe(400);
    const body = await response.json();
    // Library's ValidationErrors envelope: { type, statusCode, error: { Field: [msg, ...] } }
    expect(body.statusCode).toBe(400);
    expect(body.error.Name).toEqual(expect.arrayContaining([expect.stringMatching(/required/i)]));
  });

  test('ValidateSlugAsync rejects slug that normalizes to empty', async ({ request }) => {
    const response = await request.post('/api/Tags', {
      data: { name: unique('Tag'), slug: '!!!---' },
    });
    expect(response.status()).toBe(400);
    const body = await response.json();
    expect(body.error.Slug).toEqual(expect.arrayContaining([expect.stringMatching(/alphanumeric/i)]));
  });

  test('uniqueness is enforced asynchronously against the database', async ({ request }) => {
    const name = unique('Persistent');
    const slug = `persistent-${Date.now().toString(36)}`;

    const first = await request.post('/api/Tags', { data: { name, slug } });
    expect(first.status()).toBe(201);

    const duplicate = await request.post('/api/Tags', { data: { name, slug: unique('other') } });
    expect(duplicate.status()).toBe(400);
    expect((await duplicate.json()).error.Name).toBeTruthy();

    const duplicateSlug = await request.post('/api/Tags', {
      data: { name: unique('Other Name'), slug },
    });
    expect(duplicateSlug.status()).toBe(400);
    expect((await duplicateSlug.json()).error.Slug).toBeTruthy();
  });

  test('PATCH skips per-field hooks for omitted fields (partial semantics)', async ({ request }) => {
    const name = unique('Patchable');
    const create = await request.post('/api/Tags', {
      data: { name, slug: unique('patchable-slug').toLowerCase() },
    });
    expect(create.status()).toBe(201);
    const id = (await create.json()).id;

    // Re-PATCHing the same Name onto the same row used to fail "uniqueness" in naive
    // implementations. The Validate{X}Async hooks receive context.EntityId and exclude
    // the row's own id, so this succeeds.
    const patch = await request.patch(`/api/Tags/${id}`, { data: { name } });
    expect(patch.status()).toBe(200);
    expect((await patch.json()).name).toBe(name);
  });

  test('cross-field ValidateAsync rejects when normalized Name equals Slug', async ({ request }) => {
    // Choose values where per-field uniqueness passes (nothing else in the DB has these),
    // but the normalized form of Name equals Slug exactly — that's the cross-field rule's
    // collision condition. Run-unique stamp avoids piling up failed rows over reruns.
    const stamp = Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
    const name = `Cross ${stamp}`;          // normalizes (trim + lower + kebab) to `cross-{stamp}`
    const slug = `cross-${stamp}`;          // already in the normalized form
    const response = await request.post('/api/Tags', { data: { name, slug } });
    expect(response.status()).toBe(400);
    const body = await response.json();
    expect(body.error.Name).toEqual(expect.arrayContaining([expect.stringMatching(/differ/i)]));
  });

  test('full CRUD round-trip', async ({ request }) => {
    const name = unique('Crud');
    const create = await request.post('/api/Tags', {
      data: { name, slug: unique('crud-slug').toLowerCase() },
    });
    const id = (await create.json()).id;

    expect((await request.get(`/api/Tags/${id}`)).status()).toBe(200);

    const put = await request.put(`/api/Tags/${id}`, {
      data: { name: unique('Put'), slug: unique('put-slug').toLowerCase() },
    });
    expect(put.status()).toBe(200);

    expect((await request.delete(`/api/Tags/${id}`)).status()).toBe(204);
    expect((await request.get(`/api/Tags/${id}`)).status()).toBe(404);
  });
});
