import { defineConfig, devices } from '@playwright/test';

const BASE_URL = process.env.SAMPLE_PROJECT_URL ?? 'http://localhost:8000';

export default defineConfig({
  testDir: './tests',
  // Sequential by default: the sample shares one database, and tests rely on
  // exact counts on collection endpoints. Easy to relax once tests are isolated.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: process.env.CI
    ? [['html', { open: 'never' }], ['github']]
    : [['html', { open: 'on-failure' }], ['list']],
  use: {
    baseURL: BASE_URL,
    extraHTTPHeaders: { Accept: 'application/json' },
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  // Locally: spawn the sample under test and reuse it across reruns.
  // In CI: the workflow starts the sample explicitly, so don't double-start.
  webServer: process.env.CI
    ? undefined
    : {
        command: 'dotnet run --no-launch-profile --project ../sample-project/src/SampleProject.csproj',
        url: `${BASE_URL}/swagger/v1/swagger.json`,
        reuseExistingServer: true,
        timeout: 180_000,
        stdout: 'pipe',
        stderr: 'pipe',
      },
});
