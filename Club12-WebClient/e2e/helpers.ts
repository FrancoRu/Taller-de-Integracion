import { expect, Locator, Page } from '@playwright/test';

/** Dev-seed admin used across the whole suite; this account has ADMIN role. */
export const ADMIN_CREDENTIALS = {
  email: 'admin@club12.com',
  password: 'Admin@123456!',
};

/**
 * Console noise that's expected/benign in this dev stack and shouldn't
 * fail a "no console errors" assertion: React DevTools' info banner (not
 * an error, but some setups surface it via console.error), Vite HMR
 * connection chatter, and the browser's native favicon 404 (no favicon
 * file in this project).
 */
const IGNORED_CONSOLE_PATTERNS = [/Download the React DevTools/i, /\[vite\]/i, /favicon\.ico/i];

/**
 * Starts collecting console errors and uncaught page errors for `page`.
 * Call the returned function at the end of a test to assert none were
 * seen (filtered through IGNORED_CONSOLE_PATTERNS first).
 */
export const collectConsoleErrors = (page: Page): (() => string[]) => {
  const errors: string[] = [];

  page.on('console', msg => {
    if (msg.type() !== 'error') return;
    const text = msg.text();
    if (IGNORED_CONSOLE_PATTERNS.some(p => p.test(text))) return;
    errors.push(text);
  });

  page.on('pageerror', err => {
    errors.push(err.message);
  });

  return () => errors;
};

/** Logs the given page in as admin and waits for the panel shell to load. */
export const loginAsAdmin = async (page: Page): Promise<void> => {
  await page.goto('/login');
  await page.getByLabel('Usuario').fill(ADMIN_CREDENTIALS.email);
  await page.getByLabel('Contraseña').fill(ADMIN_CREDENTIALS.password);
  await page.getByRole('button', { name: 'Iniciar Sesión' }).click();
  await page.waitForURL('**/panel');
};

/** XPath fragment: the nearest ancestor `<div class="... MuiBox-root ...">`. */
const NEAREST_MUI_BOX =
  'xpath=ancestor::div[contains(concat(" ", normalize-space(@class), " "), " MuiBox-root ")][1]';

/** Scopes every wizard interaction below to the wizard's single Card body, so sidebar nav buttons/links never collide with in-form controls of the same name. */
export const wizardContent = (page: Page): Locator => page.locator('.MuiCardContent-root');

export const gotoWizard = async (page: Page): Promise<void> => {
  await page.goto('/panel/torneos/asistente');
  await expect(page.getByText('Asistente de creación de torneo')).toBeVisible();
};

export interface TournamentStepInput {
  name: string;
  description: string;
  startDate: string; // yyyy-mm-dd
  teamRegistrationDeadline: string; // yyyy-mm-dd
}

/**
 * Step 1 — Torneo: fills the tournament's own fields and advances. The
 * "Mín./Máx. equipos" fields were removed from this step (HU-34/HU-39): the
 * wizard no longer caps team counts, so there is nothing to fill here.
 */
export const fillTournamentStep = async (
  page: Page,
  input: TournamentStepInput
): Promise<void> => {
  const content = wizardContent(page);
  await content.getByLabel('Nombre').fill(input.name);
  await content.getByLabel('Descripción').fill(input.description);
  await content.getByLabel('Inicio').fill(input.startDate);
  await content.getByLabel('Límite de inscripción').fill(input.teamRegistrationDeadline);
  await content.getByRole('button', { name: 'Continuar' }).click();
};

/**
 * Step 2 — Equipos: selects the given teams by exact chip label. Every team
 * chip is labelled `"<team name> (<current tournament name>)"` when the team
 * already belongs to a tournament, or just `"<team name>"` when it doesn't
 * (see EquiposStep.tsx) — callers pass the exact label they expect so a
 * team accidentally reassigned from the wrong season fails loudly instead
 * of silently matching a same-named team.
 */
export const selectTeams = async (page: Page, chipLabels: string[]): Promise<void> => {
  const content = wizardContent(page);
  // The team chip list loads asynchronously after the step mounts; wait for
  // at least one chip so a slow /api/teams response doesn't race the clicks.
  await expect(content.locator('.MuiChip-root').first()).toBeVisible({ timeout: 20_000 });
  for (const label of chipLabels) {
    await content.getByRole('button', { name: label, exact: true }).click();
  }
  await content.getByRole('button', { name: 'Continuar' }).click();
};

/** Selects an MUI `TextField select` option by its exact visible option text. Assumes only one popup is open at a time. */
const chooseSelectOption = async (
  page: Page,
  scope: Locator,
  fieldLabel: string,
  optionText: string
): Promise<void> => {
  await scope.getByLabel(fieldLabel, { exact: true }).click();
  await page.getByRole('option', { name: optionText, exact: true }).click();
};

export interface RoundInput {
  /** Exact option text in the "Ronda" select, e.g. "Semifinal", "Final". */
  stageType: string;
  /** Exact option text in the "Formato" select, e.g. "Partido único". */
  bestOf?: string;
}

export interface CupInput {
  name: string;
  rounds: RoundInput[];
}

/**
 * Fills one cup box (name + rounds) that's already been added via "Agregar
 * copa" inside `scope` (a zone or the cross-cup box). The cup's first round
 * exists by default (defaults to "Final"/"Partido único" — see
 * createEmptyRound); every extra round needs an explicit "Agregar ronda".
 */
const fillCup = async (page: Page, scope: Locator, cup: CupInput): Promise<void> => {
  await scope.getByRole('button', { name: 'Agregar copa' }).click();

  const cupNameInputs = scope.getByLabel('Nombre de la copa (libre)');
  const cupBox = cupNameInputs.nth((await cupNameInputs.count()) - 1).locator(NEAREST_MUI_BOX);
  await cupBox.getByLabel('Nombre de la copa (libre)').fill(cup.name);

  for (const [index, round] of cup.rounds.entries()) {
    if (index > 0) {
      await cupBox.getByRole('button', { name: 'Agregar ronda' }).click();
    }
    const rondaSelects = cupBox.getByLabel('Ronda', { exact: true });
    const roundIdx = (await rondaSelects.count()) - 1;
    await rondaSelects.nth(roundIdx).click();
    await page.getByRole('option', { name: round.stageType, exact: true }).click();

    if (round.bestOf) {
      const formatoSelects = cupBox.getByLabel('Formato', { exact: true });
      await formatoSelects.nth(roundIdx).click();
      await page.getByRole('option', { name: round.bestOf, exact: true }).click();
    }
  }
};

export interface ZoneInput {
  name: string;
  /** Team chip labels (plain team names — DivisionesStep shows no tournament suffix). */ teamNames: string[];
  roundRobinLegs?: string; // exact option text, e.g. "2 veces"
  cups: CupInput[];
}

/**
 * Step 3 — Divisiones: adds one zone via "Agregar zona", scopes every
 * subsequent interaction to that zone's own box (so two zones can safely
 * reuse the same cup names, e.g. "Oro"/"Plata" in every zone) and fills it
 * in completely.
 */
export const addZone = async (page: Page, zone: ZoneInput): Promise<void> => {
  const content = wizardContent(page);
  await content.getByRole('button', { name: 'Agregar zona' }).click();

  const zoneNameInputs = content.getByLabel('Nombre de la zona (libre)');
  const zoneBox = zoneNameInputs.nth((await zoneNameInputs.count()) - 1).locator(NEAREST_MUI_BOX);
  await zoneBox.getByLabel('Nombre de la zona (libre)').fill(zone.name);

  for (const teamName of zone.teamNames) {
    await zoneBox.getByRole('button', { name: teamName, exact: true }).click();
  }

  if (zone.roundRobinLegs) {
    await chooseSelectOption(page, zoneBox, 'Veces que se enfrenta cada par', zone.roundRobinLegs);
  }

  for (const cup of zone.cups) {
    await fillCup(page, zoneBox, cup);
  }
};

export const continueFromDivisiones = async (page: Page): Promise<void> => {
  await wizardContent(page).getByRole('button', { name: 'Continuar' }).click();
};

export interface CrossCupInput {
  name: string;
  /**
   * Explicit team plain-names for the cup. Omit to leave "incluir todos
   * los equipos inscriptos" on (the default) so every team picked in step
   * 2 plays it; pass a list to turn that switch off and hand-pick teams
   * instead (e.g. to stay under a stage's team-count cap when step 2
   * registered more teams than the cross-cup's own group stage can hold).
   */
  teamNames?: string[];
  roundRobinLegs?: string;
  cups: CupInput[];
}

/** Step 4 — Copa cruzada: enables it, fills its name, and either leaves "incluir todos los equipos" on (default) or hand-picks `teamNames`. */
export const fillCrossCup = async (page: Page, input: CrossCupInput): Promise<void> => {
  const content = wizardContent(page);
  // MUI's Switch renders role="switch" (not "checkbox"), and Playwright's
  // check()/uncheck() only special-case checkbox/radio inputs, so a plain
  // click is the reliable way to toggle it.
  await content.getByRole('switch', { name: 'Incluir una copa cruzada entre zonas' }).click();

  const nameField = content.getByLabel('Nombre de la copa (libre)');
  await nameField.fill(input.name);
  const crossCupBox = nameField.locator(NEAREST_MUI_BOX);

  if (input.teamNames) {
    await crossCupBox
      .getByRole('switch', { name: 'Incluir todos los equipos inscriptos' })
      .click();
    for (const teamName of input.teamNames) {
      await crossCupBox.getByRole('button', { name: teamName, exact: true }).click();
    }
  }

  if (input.roundRobinLegs) {
    await chooseSelectOption(
      page,
      crossCupBox,
      'Veces que se enfrenta cada par',
      input.roundRobinLegs
    );
  }

  for (const cup of input.cups) {
    await fillCup(page, crossCupBox, cup);
  }

  await content.getByRole('button', { name: 'Continuar' }).click();
};

export const skipCrossCup = async (page: Page): Promise<void> => {
  await wizardContent(page).getByRole('button', { name: 'Continuar' }).click();
};

/**
 * Step 5 — Revisión: submits the wizard and waits for the FINAL
 * success/warning SweetAlert2 dialog, then confirms it. Returns the
 * dialog's title + body text so callers can assert on it.
 *
 * submitWizard.ts makes many sequential addDivision/addStage/etc. calls,
 * and several of those hooks show their own transient, auto-dismissing
 * "<X> creada exitosamente" toast on success (see error.context.tsx's
 * setMessage: `timer: 1500, showConfirmButton: false`). Those share the
 * same `.swal2-popup`/`.swal2-title` markup as the one real result dialog
 * (TournamentWizardPage's notifySuccess/notifyWarning), which never sets a
 * timer and always shows a confirm button — so waiting for `.swal2-confirm`
 * to appear (rather than just "some title text") is what actually
 * distinguishes the terminal dialog from an interim toast.
 */
export const submitWizardAndConfirm = async (page: Page): Promise<string> => {
  await wizardContent(page)
    .getByRole('button', { name: 'Crear torneo completo' })
    .click();

  // submitWizard.ts's sequential chain (register teams, then per zone:
  // division + group stage + team assignment + match generation + every
  // cup round, times 4 zones plus the cross-cup) is dozens of awaited API
  // calls, several of which also pop their own transient 1.5s toast (see
  // error.context.tsx's setMessage) — those keep re-triggering Swal.fire()
  // faster than each toast's own close timer, so the popup only settles
  // once the whole chain actually finishes and the terminal
  // notifySuccess/notifyWarning call (no timer) takes over. That can
  // genuinely take minutes end-to-end, not seconds — hence the long
  // timeout instead of a network-idle wait (Vite's HMR websocket never
  // goes idle, so `networkidle` doesn't apply here).
  const popup = page.locator('.swal2-popup');
  const confirmButton = popup.locator('.swal2-confirm');
  await expect(confirmButton).toBeVisible({ timeout: 240_000 });
  const title = (await popup.locator('.swal2-title').innerText()).trim();
  const html = (await popup.locator('.swal2-html-container').innerText()).trim();
  await confirmButton.click();
  return `${title} ${html}`.trim();
};
