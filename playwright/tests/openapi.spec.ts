import { expect, test } from '@playwright/test';

/**
 * Contract assertions against the OpenAPI document exposed by Swashbuckle.
 *
 * Pins the consumer-visible surface: every controller in sample-project/Commands/ApiCommand.cs
 * must produce both a collection path (with GET/POST/DELETE) and an item path (with
 * GET/PATCH/PUT/DELETE), even when the action is disabled by ActionOptions — disabled
 * actions stay listed because we gate inline with 405 instead of [NonAction].
 */
test.describe('OpenAPI contract', () => {
  const resources = [
    'Categories',
    'Restaurants',
    'RestaurantProfiles',
    'Ingredients',
    'MenuItems',
    'Gifts',
    'Tags',
    'AuditLogs',
    'TenantNotes',
  ];

  test('document is reachable and well-formed', async ({ request }) => {
    const response = await request.get('/swagger/v1/swagger.json');
    expect(response.status()).toBe(200);

    const doc = await response.json();
    expect(doc.openapi).toMatch(/^3\./);
    expect(doc.info?.title).toBe('SampleProject API');
    expect(doc.paths).toBeTruthy();
  });

  test('every resource exposes a collection path with GET/POST/DELETE', async ({ request }) => {
    const doc = await (await request.get('/swagger/v1/swagger.json')).json();

    for (const resource of resources) {
      const path = doc.paths[`/api/${resource}`];
      expect(path, `missing collection path for ${resource}`).toBeTruthy();
      expect(Object.keys(path).sort()).toEqual(['delete', 'get', 'post']);
    }
  });

  test('every resource exposes an item path with GET/PATCH/PUT/DELETE', async ({ request }) => {
    const doc = await (await request.get('/swagger/v1/swagger.json')).json();

    for (const resource of resources) {
      const path = doc.paths[`/api/${resource}/{id}`];
      expect(path, `missing item path for ${resource}`).toBeTruthy();
      expect(Object.keys(path).sort()).toEqual(['delete', 'get', 'patch', 'put']);
    }
  });

  test('disabled actions stay listed (AllowDelete=false on MenuItems still ships DELETE in the schema)', async ({ request }) => {
    // The library gates inline with 405 instead of [NonAction] so the endpoint stays
    // visible in OpenAPI / OPTIONS — documented but off by default.
    const doc = await (await request.get('/swagger/v1/swagger.json')).json();
    expect(doc.paths['/api/MenuItems/{id}']?.delete).toBeTruthy();
  });

  test('append-only AuditLogs still ships every verb in the schema (all disabled = inline 405)', async ({ request }) => {
    const doc = await (await request.get('/swagger/v1/swagger.json')).json();
    // Collection + item paths exist with the full verb set even though every mutating
    // action is disabled on the controller — same 405-inline contract as MenuItems above.
    expect(Object.keys(doc.paths['/api/AuditLogs']).sort()).toEqual(['delete', 'get', 'post']);
    expect(Object.keys(doc.paths['/api/AuditLogs/{id}']).sort()).toEqual(['delete', 'get', 'patch', 'put']);
  });
});
