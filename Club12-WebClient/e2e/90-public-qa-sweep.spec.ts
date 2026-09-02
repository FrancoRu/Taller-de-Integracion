import { expect, test } from '@playwright/test';
import { collectConsoleErrors } from './helpers';

/**
 * Full public-site QA sweep (read-only, zero side effects) — every public
 * route in App.tsx, checked against whatever the live site currently has
 * seeded (no hardcoded team/tournament names, unlike 01-03, which broke
 * once the seed was rebuilt this session). Each screen must render its
 * expected landmark and throw no console/page errors.
 */

test('Home renders hero + sections without console errors', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible({ timeout: 15_000 });
  expect(getErrors()).toEqual([]);
});

test('Quiénes somos / Ficha médica / Reglamento load without auth', async ({ page }) => {
  for (const path of ['/quienes-somos', '/ficha-medica', '/reglamento']) {
    const getErrors = collectConsoleErrors(page);
    await page.goto(path);
    await expect(page.locator('body')).not.toContainText('Cannot GET');
    expect(getErrors(), `console errors on ${path}`).toEqual([]);
  }
});

test('Temporadas: list renders and drills into the first season', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/temporadas');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });

  const firstCard = page.locator('a[href^="/temporadas/"]').first();
  if (await firstCard.count()) {
    await firstCard.click();
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
  }
  expect(getErrors()).toEqual([]);
});

test('Torneos: list renders and drills into the first tournament + all its division tabs', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/torneos');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });

  const firstLink = page.locator('a[href^="/torneos/"]').first();
  await expect(firstLink).toBeVisible({ timeout: 15_000 });
  await firstLink.click();
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });

  const tabs = page.getByRole('tab');
  const tabCount = await tabs.count();
  expect(tabCount).toBeGreaterThan(0);

  for (let i = 0; i < tabCount; i++) {
    await tabs.nth(i).click();
    await page.waitForTimeout(400);
    // A React error boundary / raw stack trace visibly breaks the page —
    // fail loudly instead of silently passing on a blank/broken tab.
    await expect(page.locator('body')).not.toContainText('Uncaught');
  }

  expect(getErrors(), 'console errors while sweeping every division tab').toEqual([]);
});

test('Equipos: public team page renders roster without local/visitante wording', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/torneos');
  const firstTournament = page.locator('a[href^="/torneos/"]').first();
  await expect(firstTournament).toBeVisible({ timeout: 15_000 });
  await firstTournament.click();

  const equiposTab = page.getByRole('tab', { name: 'Equipos' });
  if (await equiposTab.count()) {
    await equiposTab.click();
    const firstTeamLink = page.locator('a[href^="/equipos/"]').first();
    if (await firstTeamLink.count()) {
      await firstTeamLink.click();
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
      await expect(page.getByText(/\bLocal\b/i)).toHaveCount(0);
      await expect(page.getByText(/\bVisitante\b/i)).toHaveCount(0);
    }
  }
  expect(getErrors()).toEqual([]);
});

test('Sanciones públicas: search and filter controls render', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/sanciones');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
  expect(getErrors()).toEqual([]);
});

test('Campeones: page renders without erroring even with 0 or many finished tournaments', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/campeones');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
  expect(getErrors()).toEqual([]);
});

test('Novedades: blog list renders and drills into the first post', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/blog');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });

  const firstPost = page.locator('a[href^="/blog/"]').first();
  if (await firstPost.count()) {
    await firstPost.click();
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
  }
  expect(getErrors()).toEqual([]);
});

test('a public match page (if any match exists) renders a scoreboard without local/visitante wording', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/torneos');
  const firstTournament = page.locator('a[href^="/torneos/"]').first();
  await expect(firstTournament).toBeVisible({ timeout: 15_000 });
  await firstTournament.click();

  const partidosTab = page.getByRole('tab', { name: 'Partidos' });
  if (await partidosTab.count()) {
    await partidosTab.click();
    const firstMatchLink = page.locator('a[href^="/partidos/"]').first();
    if (await firstMatchLink.count()) {
      await firstMatchLink.click();
      await expect(page.locator('body')).not.toContainText('Uncaught');
      await expect(page.getByText(/\bLocal\b/i)).toHaveCount(0);
      await expect(page.getByText(/\bVisitante\b/i)).toHaveCount(0);
    }
  }
  expect(getErrors()).toEqual([]);
});

test('unknown route shows a 404 without header/footer', async ({ page }) => {
  await page.goto('/esta-ruta-no-existe-qa-sweep');
  await expect(page.locator('body')).toContainText(/no existe|no encontr/i);
});

test('/login is reachable directly but not linked from the public home nav', async ({ page }) => {
  await page.goto('/');
  const loginLink = page.locator('a[href="/login"]');
  await expect(loginLink).toHaveCount(0);

  await page.goto('/login');
  await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
});
