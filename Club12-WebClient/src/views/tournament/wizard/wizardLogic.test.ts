import { describe, expect, it } from 'vitest';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  CrossCupConfig,
  CupConfig,
  WizardState,
  ZoneConfig,
  createInitialWizardState,
} from './types';
import {
  buildWizardTree,
  isWizardReadyToSubmit,
  validateCrossCupStep,
  validateTournamentStep,
  validateZonesStep,
} from './wizardLogic';

const makeValidState = (): WizardState => {
  const state = createInitialWizardState();
  state.tournament = {
    name: 'Apertura 2026',
    description: '',
    startDate: '2026-03-01',
    teamRegistrationDeadline: '2026-02-15',
    category: TournamentCategory.Masculine,
    seasonId: 'season-1',
  };
  state.zones = [
    {
      id: 'zone-1',
      name: 'Zona A',
      hasGroupStage: true,
      roundRobinLegs: 1,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
    },
    {
      id: 'zone-2',
      name: 'Zona B',
      hasGroupStage: true,
      roundRobinLegs: 1,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
    },
  ];
  return state;
};

describe('validateTournamentStep', () => {
  it('accepts a fully filled, consistent tournament step', () => {
    expect(validateTournamentStep(makeValidState())).toEqual([]);
  });

  it('rejects a tournament with no season (a tournament always belongs to one)', () => {
    const state = makeValidState();
    state.tournament.seasonId = '';
    expect(validateTournamentStep(state).length).toBeGreaterThan(0);
  });

  it('rejects a missing name', () => {
    const state = makeValidState();
    state.tournament.name = '   ';
    expect(validateTournamentStep(state).length).toBeGreaterThan(0);
  });

  it('rejects a registration deadline on or after the start date', () => {
    const state = makeValidState();
    state.tournament.teamRegistrationDeadline = state.tournament.startDate;
    expect(validateTournamentStep(state).length).toBeGreaterThan(0);
  });
});

describe('validateZonesStep', () => {
  it('accepts named zones with valid cups (no teams are assigned in the wizard)', () => {
    expect(validateZonesStep(makeValidState())).toEqual([]);
  });

  it('rejects when there are no zones at all', () => {
    const state = makeValidState();
    state.zones = [];
    expect(validateZonesStep(state).length).toBeGreaterThan(0);
  });

  it('rejects a zone with no name', () => {
    const state = makeValidState();
    state.zones[0].name = '   ';
    expect(validateZonesStep(state).some(e => e.includes('necesitan un nombre'))).toBe(true);
  });

  it('rejects two zones with the same name', () => {
    const state = makeValidState();
    state.zones[1].name = 'zona a';
    expect(validateZonesStep(state).some(e => e.includes('dos zonas llamadas'))).toBe(true);
  });

  it('rejects a cup with no name', () => {
    const state = makeValidState();
    const cup: CupConfig = { id: 'cup-1', name: '', qualifiers: 4, bestOfByStage: {}, hasThirdPlace: true };
    state.zones[0].cups.push(cup);
    expect(validateZonesStep(state).some(e => e.includes('necesita un nombre'))).toBe(true);
  });

  it('HU-112: rejects a cup with fewer than 2 qualifiers', () => {
    const state = makeValidState();
    const cup: CupConfig = { id: 'cup-1', name: 'Copa de Oro', qualifiers: 1, bestOfByStage: {}, hasThirdPlace: true };
    state.zones[0].cups.push(cup);
    expect(validateZonesStep(state).some(e => e.includes('clasificados'))).toBe(true);
  });
});

describe('validateCrossCupStep', () => {
  it('is a no-op when disabled', () => {
    const state = makeValidState();
    expect(validateCrossCupStep(state)).toEqual([]);
  });

  it('requires a name when enabled', () => {
    const state = makeValidState();
    state.crossCup = { ...state.crossCup, enabled: true, name: '' };
    expect(validateCrossCupStep(state).length).toBeGreaterThan(0);
  });

  it('accepts an enabled cross cup with a name and at least one cup (no teams are assigned in the wizard)', () => {
    const state = makeValidState();
    state.crossCup = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa cruzada',
      cups: [{ id: 'cup-1', name: 'Copa Club12', qualifiers: 4, bestOfByStage: {}, hasThirdPlace: true }],
    };
    expect(validateCrossCupStep(state)).toEqual([]);
  });

  // HU-47: the cross cup always has a playoff — it can never be saved as
  // groups only. This was a real gap (found auditing historias-de-usuario.md
  // against the code): the state starts with cups: [], and nothing blocked
  // submitting it that way.
  it('rejects a cross cup with zero cups — playoff is mandatory', () => {
    const state = makeValidState();
    state.crossCup = { ...state.crossCup, enabled: true, name: 'Copa cruzada', cups: [] };
    expect(
      validateCrossCupStep(state).some(e => e.includes('al menos una copa de playoff'))
    ).toBe(true);
  });

  // HU-110: the cross cup is a multi-group competition.
  it('rejects fewer than one group', () => {
    const state = makeValidState();
    state.crossCup = { ...state.crossCup, enabled: true, name: 'Copa cruzada', groupCount: 0 };
    expect(validateCrossCupStep(state).some(e => e.includes('al menos un grupo'))).toBe(true);
  });

  it('rejects fewer than one qualifier per group', () => {
    const state = makeValidState();
    state.crossCup = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa cruzada',
      qualifiersPerGroup: 0,
    };
    expect(
      validateCrossCupStep(state).some(e => e.includes('al menos un equipo por grupo'))
    ).toBe(true);
  });

  it('accepts several groups with several qualifiers each', () => {
    const state = makeValidState();
    state.crossCup = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa cruzada',
      groupCount: 4,
      qualifiersPerGroup: 2,
      cups: [{ id: 'cup-1', name: 'Copa Club12', qualifiers: 8, bestOfByStage: {}, hasThirdPlace: true }],
    };
    expect(validateCrossCupStep(state)).toEqual([]);
  });
});

describe('isWizardReadyToSubmit', () => {
  it('is true only when every step is valid', () => {
    expect(isWizardReadyToSubmit(makeValidState())).toBe(true);

    const broken = makeValidState();
    broken.zones = [];
    expect(isWizardReadyToSubmit(broken)).toBe(false);
  });
});

describe('buildWizardTree', () => {
  it('emits the tournament node, one node per zone, and a group-stage line per zone with a group stage', () => {
    const state = makeValidState();
    const nodes = buildWizardTree(state);

    expect(nodes[0]).toMatchObject({ depth: 1 });
    expect(nodes[0].label).toContain('Apertura 2026');
    expect(nodes.some(n => n.depth === 2 && n.label === 'Zona A')).toBe(true);
    expect(nodes.some(n => n.depth === 3 && n.label.includes('Fase de grupos'))).toBe(true);
  });

  it('describes each cup with its rounds and best-of format', () => {
    const state = makeValidState();
    const zone: ZoneConfig = state.zones[0];
    zone.cups.push({
      id: 'cup-1',
      name: 'Copa de Oro',
      qualifiers: 4,
      bestOfByStage: {},
      hasThirdPlace: true,
    });

    const nodes = buildWizardTree(state);
    expect(
      nodes.some(
        n =>
          n.label ===
          'Copa de Oro — 4 clasifican (Semifinal al mejor de 3 → Tercer Puesto a partido único → Final al mejor de 3)'
      )
    ).toBe(true);
  });

  it('includes the cross-division cup as a tagged node when enabled', () => {
    const state = makeValidState();
    const crossCup: CrossCupConfig = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa Club12',
    };
    state.crossCup = crossCup;

    const nodes = buildWizardTree(state);
    const crossNode = nodes.find(n => n.id === 'cross-cup');
    expect(crossNode).toMatchObject({ depth: 2, tag: 'división cruzada' });
    expect(crossNode?.label).toContain('Copa Club12');
  });

  // HU-110: the cross cup review lists one line per group plus how many
  // teams advance from each, instead of a single "Fase de grupos" line.
  it('lists one line per cross-cup group and a qualifiers-per-group line', () => {
    const state = makeValidState();
    state.crossCup = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa Club12',
      groupCount: 3,
      qualifiersPerGroup: 2,
      roundRobinLegs: 2,
    };

    const nodes = buildWizardTree(state);
    const groupLabels = nodes
      .filter(n => n.id.startsWith('cross-cup-group-'))
      .map(n => n.label);

    expect(groupLabels).toHaveLength(3);
    expect(groupLabels[0]).toContain('Grupo 1');
    expect(groupLabels[0]).toContain('2 veces');
    expect(
      nodes.some(n => n.id === 'cross-cup-qualifiers' && n.label.includes('2 equipos por grupo'))
    ).toBe(true);
  });
});
