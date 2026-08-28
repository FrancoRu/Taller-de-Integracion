import { expect, test } from '@playwright/test';
import {
  addZone,
  continueFromDivisiones,
  fillTournamentStep,
  gotoWizard,
  loginAsAdmin,
  selectTeams,
  skipCrossCup,
  submitWizardAndConfirm,
} from './helpers';
import { FEMENINO_TEAMS, FEMENINO_TOURNAMENT_NAME, withTournamentSuffix } from './fixtures';

test('wizard creates Femenino Clausura: its own tournament, 1 zone, group + Oro-only playoff', async ({
  page,
}) => {
  await loginAsAdmin(page);
  await gotoWizard(page);

  await fillTournamentStep(page, {
    name: 'Femenino Clausura',
    description: 'Torneo femenino Club12 La Vuelta, temporada 2026 — Clausura.',
    startDate: '2026-10-01',
    teamRegistrationDeadline: '2026-09-15',
  });

  await selectTeams(page, withTournamentSuffix(FEMENINO_TEAMS, FEMENINO_TOURNAMENT_NAME));

  // A single zone, no Plata bracket — mirrors how Femenino was split out
  // from Apertura as its own tournament with Oro only.
  await addZone(page, {
    name: 'Femenino',
    teamNames: FEMENINO_TEAMS,
    roundRobinLegs: '2 veces',
    cups: [{ name: 'Oro', rounds: [{ stageType: 'Semifinal' }, { stageType: 'Final' }] }],
  });
  await continueFromDivisiones(page);

  // No cross-division cup for this tournament.
  await skipCrossCup(page);

  const resultText = await submitWizardAndConfirm(page);
  expect(resultText).toContain('Torneo creado');
  expect(resultText).not.toContain('observaciones');

  await expect(page).toHaveURL(/\/panel\/torneos\/[0-9a-f-]{36}/);
});
