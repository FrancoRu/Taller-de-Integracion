import { defineConfig, devices } from '@playwright/test';

/**
 * E2E config for the admin wizard + public-page regression suite.
 *
 * These tests drive a real, already-running dev stack (Vite dev server +
 * the shared dev Postgres-backed API) rather than starting their own
 * ephemeral servers — the wizard writes tournament/division/stage data
 * that is meant to be inspected afterward, so a throwaway `webServer`
 * block here would defeat the point. Start both manually before running:
 *   - backend:  already running at https://localhost:5001
 *   - frontend: `pnpm dev` (defaults to http://localhost:5173)
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  // The wizard specs drive dozens of chip/select interactions in a single
  // linear flow (one tournament = one test), so the default 30s is too
  // tight; the final submit alone can take tens of seconds server-side
  // (creating divisions/stages/team assignments/match generation calls
  // sequentially, see submitWizard.ts).
  timeout: 480_000,
  expect: {
    timeout: 10_000,
  },
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
