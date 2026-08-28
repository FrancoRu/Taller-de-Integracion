import { expect, test } from '@playwright/test';
import {
  addZone,
  continueFromDivisiones,
  fillCrossCup,
  fillTournamentStep,
  gotoWizard,
  loginAsAdmin,
  selectTeams,
  submitWizardAndConfirm,
  type CupInput,
} from './helpers';
import {
  CROSS_CUP_TEAMS,
  ZONE_A_TEAMS,
  ZONE_B_TEAMS,
  ZONE_C_TEAMS,
  ZONE_D_TEAMS,
  ZONE_TEAM_CHIP_LABELS,
} from './fixtures';

/** Every zone gets a two-round (Semifinal + Final, single match each) Oro and Plata bracket — mirrors Apertura's Zona A-D playoff shape. */
const oroPlataCups = (): CupInput[] => [
  { name: 'Oro', rounds: [{ stageType: 'Semifinal' }, { stageType: 'Final' }] },
  { name: 'Plata', rounds: [{ stageType: 'Semifinal' }, { stageType: 'Final' }] },
];

test('wizard creates Clausura Club 12 2026: 4 zones (double round-robin + Oro/Plata) plus Copa Club12 Clausura', async ({
  page,
}) => {
  await loginAsAdmin(page);
  await gotoWizard(page);

  await fillTournamentStep(page, {
    name: 'Clausura Club 12 2026',
    description:
      'Liga de básquet amateur Club12 La Vuelta, temporada 2026: Zonas A-D y Copa Club12 Clausura.',
    startDate: '2026-09-01',
    teamRegistrationDeadline: '2026-08-20',
  });

  await selectTeams(page, ZONE_TEAM_CHIP_LABELS);

  await addZone(page, {
    name: 'Zona A',
    teamNames: ZONE_A_TEAMS,
    roundRobinLegs: '2 veces',
    cups: oroPlataCups(),
  });
  await addZone(page, {
    name: 'Zona B',
    teamNames: ZONE_B_TEAMS,
    roundRobinLegs: '2 veces',
    cups: oroPlataCups(),
  });
  await addZone(page, {
    name: 'Zona C',
    teamNames: ZONE_C_TEAMS,
    roundRobinLegs: '2 veces',
    cups: oroPlataCups(),
  });
  await addZone(page, {
    name: 'Zona D',
    teamNames: ZONE_D_TEAMS,
    roundRobinLegs: '2 veces',
    cups: oroPlataCups(),
  });
  await continueFromDivisiones(page);

  // Explicit team pick (not "incluir todos") — the cross-cup's own group
  // stage is capped the same way a zone's is (see fixtures.ts), so it can't
  // take all 16 zone-selected teams. One representative per zone instead.
  await fillCrossCup(page, {
    name: 'Copa Club12 Clausura',
    teamNames: CROSS_CUP_TEAMS,
    cups: [
      {
        name: 'Copa Club12 Clausura',
        rounds: [{ stageType: 'Semifinal' }, { stageType: 'Final' }],
      },
    ],
  });

  const resultText = await submitWizardAndConfirm(page);
  expect(resultText).toContain('Torneo creado');
  expect(resultText).not.toContain('observaciones');

  await expect(page).toHaveURL(/\/panel\/torneos\/[0-9a-f-]{36}/);
});
