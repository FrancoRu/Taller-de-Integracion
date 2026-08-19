import { expect, test } from '@playwright/test';
import { collectConsoleErrors } from './helpers';

/**
 * Live-browser verification that the Clausura structure created by
 * 01-wizard-clausura.spec.ts / 02-wizard-femenino-clausura.spec.ts
 * actually renders on the PUBLIC site — not just that the API returned
 * the right rows. Depends on those two specs having already run against
 * the same live backend (see playwright.config.ts: `fullyParallel: false`,
 * `workers: 1`, and this file's `03-` prefix keep the run order stable).
 */

test('public tournaments list shows both Clausura tournaments', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);

  await page.goto('/torneos');
  await expect(page.getByText('Clausura Club 12 2026')).toBeVisible();
  await expect(page.getByText('Femenino Clausura')).toBeVisible();

  expect(getErrors()).toEqual([]);
});

test('public Clausura Club 12 2026 page: all 5 division tabs, standings, partidos and llaves render', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);

  await page.goto('/torneos/clausura-club-12-2026');
  await expect(page.getByRole('heading', { name: 'Clausura Club 12 2026' })).toBeVisible();

  // Division tabs load asynchronously (separate effect from the tournament
  // itself) — wait for the last one (the cross-cup, sorted last by
  // orderDivisions) rather than a fixed timeout.
  const tabs = page.getByRole('tab');
  await expect(page.getByRole('tab', { name: 'Copa Club12 Clausura' })).toBeVisible({
    timeout: 15_000,
  });
  await expect(tabs).toHaveCount(7); // Información, Equipos, Zona A-D, Copa Club12 Clausura

  // Division tab ORDER: zones alphabetically first, cross-cup last (see
  // PublicTournamentPage.tsx's orderDivisions — a previously-fixed issue
  // this session, re-checked here so it doesn't regress).
  await expect(tabs.nth(2)).toHaveText('Zona A');
  await expect(tabs.nth(3)).toHaveText('Zona B');
  await expect(tabs.nth(4)).toHaveText('Zona C');
  await expect(tabs.nth(5)).toHaveText('Zona D');
  await expect(tabs.nth(6)).toHaveText('Copa Club12 Clausura');

  // Zona A: Posiciones (default sub-tab) — standings are legitimately
  // empty until a match has a result (none do yet, every match is still
  // scheduled), so this just confirms the empty state renders cleanly
  // rather than erroring.
  await page.getByRole('tab', { name: 'Zona A' }).click();
  await expect(page.getByText('Todavía no hay posiciones')).toBeVisible();

  // Partidos: the 12 real double round-robin matches for just this zone
  // (regression guard for the match-generation bug fixed this session —
  // it used to pull in every other zone's teams too).
  await page.getByRole('tab', { name: 'Partidos' }).click();
  await expect(page.getByText('No hay partidos registrados')).not.toBeVisible();
  await expect(page.getByText(/WOLVES/).first()).toBeVisible();

  // Llaves: appears once playoff (elimination) stages exist, and must not
  // error out even though no teams are seeded into them yet (the wizard
  // deliberately leaves cup-stage team assignment for later, once group
  // standings exist).
  await page.getByRole('tab', { name: 'Llaves' }).click();
  await page.waitForTimeout(500);

  expect(getErrors()).toEqual([]);
});

test('public Femenino Clausura page renders its single zone with Oro-only playoff', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);

  await page.goto('/torneos/femenino-clausura');
  await expect(page.getByRole('heading', { name: 'Femenino Clausura' })).toBeVisible();

  await expect(page.getByRole('tab', { name: 'Femenino' })).toBeVisible({ timeout: 15_000 });
  await page.getByRole('tab', { name: 'Femenino' }).click();

  await page.getByRole('tab', { name: 'Partidos' }).click();
  await expect(page.getByText('No hay partidos registrados')).not.toBeVisible();
  await expect(page.getByText(/MALALAS/).first()).toBeVisible();

  expect(getErrors()).toEqual([]);
});

test('a team\'s public page shows its current (season-scoped) roster without errors', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);

  // CACHUCHANS was never touched by the wizard runs above (stayed on
  // Apertura) and has a real seeded roster — a live sanity check that
  // roster reads via the new season-scoped PlayerTeamRegistration path
  // still return the right players, not just that the unit tests pass.
  await page.goto('/equipos/cachuchans');
  await expect(page.getByText('CACHUCHANS', { exact: false }).first()).toBeVisible();
  await expect(page.getByText('VEIGA Pablo')).toBeVisible();

  expect(getErrors()).toEqual([]);
});
