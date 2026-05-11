import { APIRequestContext, expect, test } from '@playwright/test';
import { unique } from '../helpers/data';

/**
 * Exercises the library's "free" list-endpoint surface — pagination, sorting, and the three
 * built-in filters (QueryStringFilter, QueryStringSearchFilter, QueryStringIdRangeFilter) —
 * plus the custom <see cref="OnlyAvailableMenuItemsFilter"/> wired on MenuItemsController.
 *
 * Tests scope themselves with the id-range filter (?ids=) so the assertions stay
 * deterministic regardless of what other specs left in the database.
 */

async function seedTags(
  request: APIRequestContext,
  prefix: string,
  count: number,
): Promise<number[]> {
  const ids: number[] = [];
  for (let i = 0; i < count; i++) {
    const idx = String(i + 1).padStart(3, '0');
    const response = await request.post('/api/Tags', {
      data: { name: `${prefix}-${idx}`, slug: `${prefix}-${idx}-slug` },
    });
    expect(response.status()).toBe(201);
    ids.push((await response.json()).id);
  }
  return ids;
}

function idsParam(ids: number[]): string {
  return ids.map((id) => `ids=${id}`).join('&');
}

test.describe('Pagination — PageNumberPagination envelope', () => {
  test('page=1 + page_size=5 over 12 ids: count, next set, previous null', async ({ request }) => {
    const prefix = unique('page');
    const ids = await seedTags(request, prefix, 12);

    const response = await request.get(`/api/Tags?${idsParam(ids)}&page=1&page_size=5`);
    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.count).toBe(12);
    expect(body.results).toHaveLength(5);
    expect(body.previous).toBeFalsy();
    expect(body.next).toContain('page=2');
  });

  test('page=3 + page_size=5: tail page returns partial results, next null', async ({ request }) => {
    const prefix = unique('page');
    const ids = await seedTags(request, prefix, 12);

    const response = await request.get(`/api/Tags?${idsParam(ids)}&page=3&page_size=5`);
    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.count).toBe(12);
    expect(body.results).toHaveLength(2);
    expect(body.next).toBeFalsy();
    expect(body.previous).toContain('page=2');
  });
});

test.describe('Sorting — ?sort= ascending, ?sortDesc= descending', () => {
  test('?sort=Name returns rows in lexicographic ascending order', async ({ request }) => {
    const prefix = unique('sort');
    // Deliberately seed out of order so the sort proves itself.
    const seeds: { name: string; slug: string }[] = [
      { name: `${prefix}-charlie`, slug: `${prefix}-c-s` },
      { name: `${prefix}-alpha`,   slug: `${prefix}-a-s` },
      { name: `${prefix}-bravo`,   slug: `${prefix}-b-s` },
    ];
    const ids: number[] = [];
    for (const s of seeds) {
      const r = await request.post('/api/Tags', { data: s });
      expect(r.status()).toBe(201);
      ids.push((await r.json()).id);
    }

    const response = await request.get(`/api/Tags?${idsParam(ids)}&sort=Name`);
    expect(response.status()).toBe(200);
    const names: string[] = (await response.json()).results.map((r: any) => r.name);
    expect(names).toEqual([`${prefix}-alpha`, `${prefix}-bravo`, `${prefix}-charlie`]);
  });

  test('?sortDesc=Name reverses the order', async ({ request }) => {
    const prefix = unique('sortdesc');
    const seeds = [
      { name: `${prefix}-alpha`,   slug: `${prefix}-a-s` },
      { name: `${prefix}-bravo`,   slug: `${prefix}-b-s` },
      { name: `${prefix}-charlie`, slug: `${prefix}-c-s` },
    ];
    const ids: number[] = [];
    for (const s of seeds) {
      const r = await request.post('/api/Tags', { data: s });
      ids.push((await r.json()).id);
    }

    const response = await request.get(`/api/Tags?${idsParam(ids)}&sortDesc=Name`);
    expect(response.status()).toBe(200);
    const names: string[] = (await response.json()).results.map((r: any) => r.name);
    expect(names).toEqual([`${prefix}-charlie`, `${prefix}-bravo`, `${prefix}-alpha`]);
  });
});

test.describe('Built-in filters', () => {
  test('QueryStringFilter — ?Name=<exact> matches a single row', async ({ request }) => {
    const target = unique('exact');
    await request.post('/api/Tags', { data: { name: target, slug: `${target}-slug` } });
    // A non-match seeded with the same prefix proves the exact-equality semantic.
    await request.post('/api/Tags', { data: { name: `${target}-decoy`, slug: `${target}-decoy-slug` } });

    const response = await request.get(`/api/Tags?Name=${encodeURIComponent(target)}`);
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.count).toBe(1);
    expect(body.results[0].name).toBe(target);
  });

  test('QueryStringSearchFilter — caller supplies LIKE wildcards explicitly', async ({ request }) => {
    // The library forwards `?search=<term>` to EF.Functions.Like(field, term) without
    // wrapping the term in `%`. Consumers wanting substring matches MUST include the
    // wildcards themselves — documented contract.
    const prefix = unique('searchhit');
    await request.post('/api/Tags', { data: { name: `${prefix}-rosemary`, slug: `${prefix}-rosemary-slug` } });
    await request.post('/api/Tags', { data: { name: `${prefix}-thyme`,    slug: `${prefix}-thyme-slug`    } });

    const response = await request.get('/api/Tags', {
      params: { search: `${prefix}%` },
    });
    expect(response.status()).toBe(200);
    expect((await response.json()).count).toBeGreaterThanOrEqual(2);
  });

  test('QueryStringIdRangeFilter — ?ids=1&ids=2 returns exactly those rows', async ({ request }) => {
    const prefix = unique('ids');
    const ids = await seedTags(request, prefix, 3);
    const wanted = [ids[0], ids[2]];

    const response = await request.get(`/api/Tags?ids=${wanted[0]}&ids=${wanted[1]}`);
    expect(response.status()).toBe(200);
    const returnedIds: number[] = (await response.json()).results.map((r: any) => r.id).sort();
    expect(returnedIds).toEqual(wanted.sort());
  });

  test('QueryStringIdRangeFilter — comma-separated form ?ids=1,2,3 also works', async ({ request }) => {
    const prefix = unique('idscsv');
    const ids = await seedTags(request, prefix, 3);

    const response = await request.get(`/api/Tags?ids=${ids.join(',')}`);
    expect(response.status()).toBe(200);
    expect((await response.json()).count).toBe(3);
  });
});

test.describe('Custom Filter<MenuItem> — OnlyAvailableMenuItemsFilter', () => {
  test('default GET returns both available and unavailable rows', async ({ request }) => {
    const restaurantCreate = await request.post('/api/Restaurants', {
      data: { name: unique('FilterHost'), address: 'a', phone: '0' },
    });
    const restaurantId = (await restaurantCreate.json()).id;

    const prefix = unique('avail');
    const onCreate = await request.post('/api/MenuItems', {
      data: { restaurantId, name: `${prefix}-on`,  description: '', price: 1, isAvailable: true },
    });
    const offCreate = await request.post('/api/MenuItems', {
      data: { restaurantId, name: `${prefix}-off`, description: '', price: 1, isAvailable: false },
    });
    const onId = (await onCreate.json()).id;
    const offId = (await offCreate.json()).id;

    const all = await request.get(`/api/MenuItems?ids=${onId}&ids=${offId}`);
    expect(all.status()).toBe(200);
    expect((await all.json()).count).toBe(2);
  });

  test('?onlyAvailable=true narrows to IsAvailable=true rows', async ({ request }) => {
    const restaurantCreate = await request.post('/api/Restaurants', {
      data: { name: unique('FilterHost'), address: 'a', phone: '0' },
    });
    const restaurantId = (await restaurantCreate.json()).id;

    const prefix = unique('availonly');
    const onCreate = await request.post('/api/MenuItems', {
      data: { restaurantId, name: `${prefix}-on`,  description: '', price: 1, isAvailable: true },
    });
    const offCreate = await request.post('/api/MenuItems', {
      data: { restaurantId, name: `${prefix}-off`, description: '', price: 1, isAvailable: false },
    });
    const onId = (await onCreate.json()).id;
    const offId = (await offCreate.json()).id;

    const onlyAvailable = await request.get(`/api/MenuItems?ids=${onId}&ids=${offId}&onlyAvailable=true`);
    expect(onlyAvailable.status()).toBe(200);
    const body = await onlyAvailable.json();
    expect(body.count).toBe(1);
    expect(body.results[0].isAvailable).toBe(true);
    expect(body.results[0].name).toBe(`${prefix}-on`);
  });
});
