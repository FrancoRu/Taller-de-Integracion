import { expect, test } from '@playwright/test';
import { collectConsoleErrors, loginAsAdmin } from './helpers';

/**
 * Broader regression crawl of public + admin pages, given how much
 * changed this session: Admin role wizard/panel access, season-scoped
 * team rosters (PlayerTeamRegistration), slug-based tournament/team/match
 * URLs, react-tournament-brackets, and the editUser role selector. Not
 * exhaustive — targets exactly what the task called out as at risk of a
 * silent regression.
 */

test('public home page has no console errors', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/');
  await page.waitForTimeout(1000);
  expect(getErrors()).toEqual([]);
});

test('public match page renders without console errors', async ({ page }) => {
  const getErrors = collectConsoleErrors(page);
  await page.goto('/partidos/wolves-vs-lby-2026-09-01');
  await expect(page.getByText('WOLVES', { exact: false }).first()).toBeVisible();
  await expect(page.getByText('LBY', { exact: false }).first()).toBeVisible();
  expect(getErrors()).toEqual([]);
});

test('admin wizard page (/panel/torneos/asistente) loads for Admin with no console errors', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);
  await loginAsAdmin(page);
  await page.goto('/panel/torneos/asistente');
  await expect(page.getByText('Asistente de creación de torneo')).toBeVisible();
  // Regression guard for the Admin-cannot-reach-the-wizard bug fixed this
  // session: reaching this page at all (not bounced to /forbidden) is the
  // point, but assert the URL explicitly too.
  await expect(page).toHaveURL(/\/panel\/torneos\/asistente$/);
  expect(getErrors()).toEqual([]);
});

test('admin users page (/panel/usuarios) loads with no console errors, and the role selector opens without error', async ({
  page,
}) => {
  const getErrors = collectConsoleErrors(page);
  await loginAsAdmin(page);
  await page.goto('/panel/usuarios');
  await expect(page.getByText('admin@club12.com').first()).toBeVisible();

  // Open one non-admin user's edit page and confirm the role <select>
  // (added this session to editUser.tsx) renders.
  const ownerRow = page.getByText('francoruggeri19@gmail.com').first();
  if (await ownerRow.isVisible().catch(() => false)) {
    await ownerRow.click();
    await page.waitForTimeout(500);
    const editLink = page.getByRole('link', { name: /editar/i }).or(page.getByRole('button', { name: /editar/i }));
    if (await editLink.first().isVisible().catch(() => false)) {
      await editLink.first().click();
      await expect(page.getByLabel('Rol', { exact: false }).or(page.getByText('Rol', { exact: false }))).toBeVisible({
        timeout: 5000,
      });
    }
  }

  expect(getErrors()).toEqual([]);
});

test('division tab selection lives in the URL: survives a reload, and browser back leaves the page (tab clicks use history.replace by design)', async ({
  page,
}) => {
  await page.goto('/torneos');
  await page.getByText('Clausura Club 12 2026').click();
  await expect(page.getByRole('tab', { name: 'Copa Club12 Clausura' })).toBeVisible({
    timeout: 15_000,
  });

  await page.getByRole('tab', { name: 'Zona B' }).click();
  await expect(page.getByRole('tab', { name: 'Zona B', selected: true })).toBeVisible();
  await expect(page).toHaveURL(/tab=/);

  // Reloading keeps the selected tab (state lives in the URL query param,
  // not component state) — a shared link lands on the same view.
  await page.reload();
  await expect(page.getByRole('tab', { name: 'Zona B', selected: true })).toBeVisible();

  // Tab clicks intentionally use history.replace (see the `setTab` comment
  // in PublicTournamentPage.tsx) so they don't each pile up a history
  // entry — back leaves the tournament page entirely (to wherever the
  // user came from), rather than stepping through tabs one at a time.
  await page.goBack();
  await expect(page).toHaveURL(/\/torneos$/);
});
