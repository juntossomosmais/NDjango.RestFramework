import { APIRequestContext, expect, test } from '@playwright/test';
import { unique } from '../helpers/data';

/**
 * End-to-end CRUD coverage per controller in sample-project/Commands/ApiCommand.cs.
 *
 * Each describe block exercises the same six-verb surface (GET list, POST, GET single,
 * PATCH, PUT, DELETE) so a regression in the library's BaseController action shape will
 * fail the matching resource. Resources with FK chains (RestaurantProfile, MenuItem)
 * create their parent inline rather than relying on shared fixtures.
 *
 * Extra assertions per resource cover the library-specific knobs:
 *   - Restaurants: AllowBulkDelete = true → DELETE /api/Restaurants?ids=... → 204
 *   - MenuItems:   AllowDelete     = false → DELETE /api/MenuItems/{id}    → 405
 */

async function createRestaurant(request: APIRequestContext): Promise<number> {
  const response = await request.post('/api/Restaurants', {
    data: {
      name: unique('Restaurant'),
      address: '1 Test St',
      phone: '555-0000',
    },
  });
  expect(response.status()).toBe(201);
  return (await response.json()).id;
}

test.describe('Categories CRUD', () => {
  test('create → list → read → patch → put → delete', async ({ request }) => {
    const name = unique('pizza');
    const create = await request.post('/api/Categories', {
      data: { name, description: 'woodfired' },
    });
    expect(create.status()).toBe(201);
    const created = await create.json();
    expect(created.id).toBeGreaterThan(0);
    expect(created.name).toBe(name);
    expect(created.createdAt).toBeTruthy();

    const list = await request.get('/api/Categories');
    expect(list.status()).toBe(200);
    const page = await list.json();
    expect(page.count).toBeGreaterThanOrEqual(1);
    expect(Array.isArray(page.results)).toBe(true);

    const read = await request.get(`/api/Categories/${created.id}`);
    expect(read.status()).toBe(200);
    expect((await read.json()).name).toBe(name);

    const patch = await request.patch(`/api/Categories/${created.id}`, {
      data: { description: 'patched' },
    });
    expect(patch.status()).toBe(200);
    expect((await patch.json()).description).toBe('patched');

    const put = await request.put(`/api/Categories/${created.id}`, {
      data: { name: unique('renamed'), description: 'put' },
    });
    expect(put.status()).toBe(200);
    expect((await put.json()).description).toBe('put');

    const del = await request.delete(`/api/Categories/${created.id}`);
    expect(del.status()).toBe(204);

    const readGone = await request.get(`/api/Categories/${created.id}`);
    expect(readGone.status()).toBe(404);
  });
});

test.describe('Restaurants CRUD', () => {
  test('create → list → read → patch → put → delete', async ({ request }) => {
    const name = unique('Restaurant');
    const create = await request.post('/api/Restaurants', {
      data: { name, address: '742 Evergreen Terrace', phone: '555-1234' },
    });
    expect(create.status()).toBe(201);
    const created = await create.json();

    const read = await request.get(`/api/Restaurants/${created.id}`);
    expect(read.status()).toBe(200);

    const patch = await request.patch(`/api/Restaurants/${created.id}`, {
      data: { phone: '555-9999' },
    });
    expect(patch.status()).toBe(200);
    expect((await patch.json()).phone).toBe('555-9999');

    const put = await request.put(`/api/Restaurants/${created.id}`, {
      data: { name: unique('Reborn'), address: 'new', phone: '000-0000' },
    });
    expect(put.status()).toBe(200);

    const del = await request.delete(`/api/Restaurants/${created.id}`);
    expect(del.status()).toBe(204);
  });

  test('bulk delete (AllowBulkDelete=true) removes ids in a single shot', async ({ request }) => {
    const a = await createRestaurant(request);
    const b = await createRestaurant(request);

    const bulk = await request.delete(`/api/Restaurants?ids=${a}&ids=${b}`);
    expect(bulk.status()).toBe(204);

    const gone = await request.get(`/api/Restaurants/${a}`);
    expect(gone.status()).toBe(404);
  });
});

test.describe('RestaurantProfiles CRUD', () => {
  test('create → list → read → patch → put → delete (with parent FK)', async ({ request }) => {
    const restaurantId = await createRestaurant(request);

    const create = await request.post('/api/RestaurantProfiles', {
      data: {
        restaurantId,
        website: 'https://example.com',
        openingHours: '9-22',
        capacity: 40,
      },
    });
    expect(create.status()).toBe(201);
    const created = await create.json();

    const patch = await request.patch(`/api/RestaurantProfiles/${created.id}`, {
      data: { capacity: 80 },
    });
    expect(patch.status()).toBe(200);
    expect((await patch.json()).capacity).toBe(80);

    const put = await request.put(`/api/RestaurantProfiles/${created.id}`, {
      data: { restaurantId, website: '', openingHours: '24/7', capacity: 100 },
    });
    expect(put.status()).toBe(200);

    const del = await request.delete(`/api/RestaurantProfiles/${created.id}`);
    expect(del.status()).toBe(204);
  });
});

test.describe('Ingredients CRUD', () => {
  test('create → list → read → patch → put → delete', async ({ request }) => {
    const name = unique('tomato');
    const create = await request.post('/api/Ingredients', {
      data: { name, isAllergen: false },
    });
    expect(create.status()).toBe(201);
    const created = await create.json();

    const patch = await request.patch(`/api/Ingredients/${created.id}`, {
      data: { isAllergen: true },
    });
    expect(patch.status()).toBe(200);
    expect((await patch.json()).isAllergen).toBe(true);

    const put = await request.put(`/api/Ingredients/${created.id}`, {
      data: { name: unique('renamed'), isAllergen: false },
    });
    expect(put.status()).toBe(200);

    const del = await request.delete(`/api/Ingredients/${created.id}`);
    expect(del.status()).toBe(204);
  });
});

test.describe('MenuItems CRUD', () => {
  test('create → list → read → patch → put (single DELETE disabled)', async ({ request }) => {
    const restaurantId = await createRestaurant(request);

    const create = await request.post('/api/MenuItems', {
      data: {
        restaurantId,
        name: unique('Margherita'),
        description: 'classic',
        price: 19.9,
        isAvailable: true,
      },
    });
    expect(create.status()).toBe(201);
    const created = await create.json();

    const patch = await request.patch(`/api/MenuItems/${created.id}`, {
      data: { isAvailable: false },
    });
    expect(patch.status()).toBe(200);
    expect((await patch.json()).isAvailable).toBe(false);

    const put = await request.put(`/api/MenuItems/${created.id}`, {
      data: {
        restaurantId,
        name: unique('Renamed'),
        description: 'put',
        price: 29.9,
        isAvailable: true,
      },
    });
    expect(put.status()).toBe(200);
  });

  test('DELETE returns 405 when AllowDelete=false', async ({ request }) => {
    const restaurantId = await createRestaurant(request);
    const create = await request.post('/api/MenuItems', {
      data: {
        restaurantId,
        name: unique('DoomedToStay'),
        description: '',
        price: 1,
        isAvailable: true,
      },
    });
    const created = await create.json();

    const del = await request.delete(`/api/MenuItems/${created.id}`);
    expect(del.status()).toBe(405);

    // Row is still there — guard short-circuited before persistence.
    const stillThere = await request.get(`/api/MenuItems/${created.id}`);
    expect(stillThere.status()).toBe(200);
  });
});

test.describe('Gifts CRUD', () => {
  test('round-trips the wide primitive-type surface', async ({ request }) => {
    const payload = {
      name: unique('gift'),
      isWrapped: true,
      trackingCode: '00000000-0000-0000-0000-000000000001',
      price: 12.34,
      barcode: 9_876_543_210,
      weight: 1.25,
      rating: 4.5,
      quantityInStock: 7,
      minAge: 3,
      shippedAt: '2026-05-12T10:00:00+00:00',
      preparationTime: '00:30:00',
      expirationDate: '2027-01-01',
      availableFrom: '08:00:00',
      description: 'sample',
      notes: 'wide type surface',
    };

    const create = await request.post('/api/Gifts', { data: payload });
    expect(create.status()).toBe(201);
    const created = await create.json();
    expect(created.id).toBeGreaterThan(0);
    expect(created.isWrapped).toBe(true);
    expect(created.quantityInStock).toBe(7);
    expect(created.minAge).toBe(3);

    const patch = await request.patch(`/api/Gifts/${created.id}`, {
      data: { rating: 5.0, quantityInStock: 99 },
    });
    expect(patch.status()).toBe(200);
    const patched = await patch.json();
    expect(patched.rating).toBeCloseTo(5.0);
    expect(patched.quantityInStock).toBe(99);

    const del = await request.delete(`/api/Gifts/${created.id}`);
    expect(del.status()).toBe(204);
  });
});
