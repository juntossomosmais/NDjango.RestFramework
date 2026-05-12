import { expect, test } from '@playwright/test';
import { unique } from '../helpers/data';

/**
 * Pins the Query-with-Include pattern: RestaurantsController sets
 *   Query = ctx.Restaurants.Include(r => r.Profile).Include(r => r.MenuItems).AsNoTracking()
 * so every action's load step sees the eager-loaded navigation, and the configured nested
 * entries in Restaurant.GetFields() ("RestaurantProfile:Website", "RestaurantProfile:Capacity")
 * project the included Profile data into the response.
 */
test.describe('Restaurants — Query with .Include() projects nested fields', () => {
  test('GET single returns nested RestaurantProfile fields when a profile exists', async ({ request }) => {
    const restaurantCreate = await request.post('/api/Restaurants', {
      data: { name: unique('Linked'), address: '1 Linked Way', phone: '555-NEST' },
    });
    expect(restaurantCreate.status()).toBe(201);
    const restaurant = await restaurantCreate.json();

    const profileCreate = await request.post('/api/RestaurantProfiles', {
      data: {
        restaurantId: restaurant.id,
        website: 'https://nested.example.com',
        openingHours: '09-23',
        capacity: 42,
      },
    });
    expect(profileCreate.status()).toBe(201);

    const fetched = await request.get(`/api/Restaurants/${restaurant.id}`);
    expect(fetched.status()).toBe(200);
    const body = await fetched.json();

    // Nested-field projection drops the values into the top-level shape that JsonTransform
    // produces. We assert presence and value — the exact JSON key the library emits.
    const haystack = JSON.stringify(body);
    expect(haystack).toContain('https://nested.example.com');
    expect(haystack).toContain('42');
  });

  test('GET single still works when no profile exists (no eager-load crash)', async ({ request }) => {
    const restaurantCreate = await request.post('/api/Restaurants', {
      data: { name: unique('Bare'), address: 'nowhere', phone: '000' },
    });
    expect(restaurantCreate.status()).toBe(201);
    const id = (await restaurantCreate.json()).id;

    const fetched = await request.get(`/api/Restaurants/${id}`);
    expect(fetched.status()).toBe(200);
  });
});

test.describe('ValidateDestroyAsync — pre-delete state predicate', () => {
  test('DELETE on a restaurant with menu items returns 400 with a non-field error', async ({ request }) => {
    const restaurantCreate = await request.post('/api/Restaurants', {
      data: { name: unique('NonEmpty'), address: '1', phone: '1' },
    });
    expect(restaurantCreate.status()).toBe(201);
    const restaurantId = (await restaurantCreate.json()).id;

    const menuItemCreate = await request.post('/api/MenuItems', {
      data: {
        restaurantId,
        name: unique('SoupOfDay'),
        description: '',
        price: 5,
        isAvailable: true,
      },
    });
    expect(menuItemCreate.status()).toBe(201);

    const del = await request.delete(`/api/Restaurants/${restaurantId}`);
    expect(del.status()).toBe(400);
    const body = await del.json();
    // ValidationErrors.NonFieldErrorsKey is "non_field_errors" (DRF convention).
    expect(body.error.non_field_errors).toEqual(
      expect.arrayContaining([expect.stringMatching(/menu items/i)]),
    );
  });

  test('DELETE on an empty restaurant still succeeds (hook only fires when predicate is true)', async ({ request }) => {
    const restaurantCreate = await request.post('/api/Restaurants', {
      data: { name: unique('Empty'), address: 'x', phone: 'y' },
    });
    const id = (await restaurantCreate.json()).id;

    expect((await request.delete(`/api/Restaurants/${id}`)).status()).toBe(204);
  });
});
