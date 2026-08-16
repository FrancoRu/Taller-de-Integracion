import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { StageType } from '@/modules/stage/type/stage.d';
import { CrossCupConfig, CupConfig, WizardState, ZoneConfig, createInitialWizardState } from './types';
import {
  buildWizardTree,
  isWizardReadyToSubmit,
  resolveCrossCupTeamIds,
  validateCrossCupStep,
  validateTeamsStep,
  validateTournamentStep,
  validateZonesStep,
} from './wizardLogic';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const makeValidState = (): WizardState => {
  const state = createInitialWizardState();
  state.tournament = {
    name: 'Apertura 2026',
    description: '',
    startDate: '2026-03-01',
    teamRegistrationDeadline: '2026-02-15',
    minTeams: 2,
    maxTeams: 8,
  };
  state.selectedTeamIds = [guid('a'), guid('b'), guid('c'), guid('d')];
  state.zones = [
    {
      id: 'zone-1',
      name: 'Zona A',
      teamIds: [guid('a'), guid('b')],
      hasGroupStage: true,
      roundRobinLegs: 1,
      cups: [],
    },
    {
      id: 'zone-2',
      name: 'Zona B',
      teamIds: [guid('c'), guid('d')],
      hasGroupStage: true,
      roundRobinLegs: 1,
      cups: [],
    },
  ];
  return state;
};

describe('validateTournamentStep', () => {
  it('accepts a fully filled, consistent tournament step', () => {
    expect(validateTournamentStep(makeValidState())).toEqual([]);
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

  it('rejects minTeams below 2', () => {
    const state = makeValidState();
    state.tournament.minTeams = 1;
    expect(validateTournamentStep(state).length).toBeGreaterThan(0);
  });

  it('rejects minTeams greater than maxTeams', () => {
    const state = makeValidState();
    state.tournament.minTeams = 10;
    state.tournament.maxTeams = 8;
    expect(validateTournamentStep(state).length).toBeGreaterThan(0);
  });
});

describe('validateTeamsStep', () => {
  it('rejects fewer teams than the configured minimum', () => {
    const state = makeValidState();
    state.tournament.minTeams = 6;
    expect(validateTeamsStep(state).length).toBeGreaterThan(0);
  });

  it('rejects more teams than the configured maximum', () => {
    const state = makeValidState();
    state.tournament.maxTeams = 2;
    expect(validateTeamsStep(state).length).toBeGreaterThan(0);
  });

  it('accepts a team count within range', () => {
    expect(validateTeamsStep(makeValidState())).toEqual([]);
  });
});

describe('validateZonesStep', () => {
  it('accepts a clean partition of every selected team across named zones', () => {
    expect(validateZonesStep(makeValidState())).toEqual([]);
  });

  it('rejects when there are no zones at all', () => {
    const state = makeValidState();
    state.zones = [];
    expect(validateZonesStep(state).length).toBeGreaterThan(0);
  });

  it('rejects an unassigned team', () => {
    const state = makeValidState();
    state.selectedTeamIds.push(guid('e'));
    expect(validateZonesStep(state).some(e => e.includes('ninguna zona'))).toBe(true);
  });

  it('rejects a team assigned to two zones at once', () => {
    const state = makeValidState();
    state.zones[1].teamIds.push(guid('a'));
    expect(validateZonesStep(state).some(e => e.includes('más de una zona'))).toBe(true);
  });

  it('rejects two zones with the same name', () => {
    const state = makeValidState();
    state.zones[1].name = 'zona a';
    expect(validateZonesStep(state).some(e => e.includes('dos zonas llamadas'))).toBe(true);
  });

  it('rejects an empty zone', () => {
    const state = makeValidState();
    state.zones.push({
      id: 'zone-3',
      name: 'Zona C',
      teamIds: [],
      hasGroupStage: true,
      roundRobinLegs: 1,
      cups: [],
    });
    expect(validateZonesStep(state).some(e => e.includes('no tiene equipos'))).toBe(true);
  });

  it('rejects a cup with no name', () => {
    const state = makeValidState();
    const cup: CupConfig = { id: 'cup-1', name: '', rounds: [{ id: 'r1', stageType: StageType.Final, bestOf: 1 }] };
    state.zones[0].cups.push(cup);
    expect(validateZonesStep(state).some(e => e.includes('necesita un nombre'))).toBe(true);
  });

  it('rejects a cup with no rounds', () => {
    const state = makeValidState();
    const cup: CupConfig = { id: 'cup-1', name: 'Copa de Oro', rounds: [] };
    state.zones[0].cups.push(cup);
    expect(validateZonesStep(state).some(e => e.includes('al menos una ronda'))).toBe(true);
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

  it('requires at least 2 teams when enabled with an explicit roster', () => {
    const state = makeValidState();
    state.crossCup = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa cruzada',
      includeAllTeams: false,
      teamIds: [guid('a')],
    };
    expect(validateCrossCupStep(state).length).toBeGreaterThan(0);
  });

  it('accepts includeAllTeams with enough selected teams', () => {
    const state = makeValidState();
    state.crossCup = { ...state.crossCup, enabled: true, name: 'Copa cruzada', includeAllTeams: true };
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

describe('resolveCrossCupTeamIds', () => {
  it('resolves to every selected team when includeAllTeams is true', () => {
    const state = makeValidState();
    state.crossCup = { ...state.crossCup, includeAllTeams: true };
    expect(resolveCrossCupTeamIds(state)).toEqual(state.selectedTeamIds);
  });

  it('resolves to the explicit roster when includeAllTeams is false', () => {
    const state = makeValidState();
    const explicit = [guid('a')];
    state.crossCup = { ...state.crossCup, includeAllTeams: false, teamIds: explicit };
    expect(resolveCrossCupTeamIds(state)).toEqual(explicit);
  });
});

describe('buildWizardTree', () => {
  it('emits the tournament node, one node per zone, and a group-stage line per zone with a group stage', () => {
    const state = makeValidState();
    const nodes = buildWizardTree(state);

    expect(nodes[0]).toMatchObject({ depth: 1, tag: '4 equipos' });
    expect(nodes.some(n => n.depth === 2 && n.label.startsWith('Zona A'))).toBe(true);
    expect(nodes.some(n => n.depth === 3 && n.label.includes('Fase de grupos'))).toBe(true);
  });

  it('describes each cup with its rounds and best-of format', () => {
    const state = makeValidState();
    const zone: ZoneConfig = state.zones[0];
    zone.cups.push({
      id: 'cup-1',
      name: 'Copa de Oro',
      rounds: [
        { id: 'r1', stageType: StageType.SemiFinal, bestOf: 3 },
        { id: 'r2', stageType: StageType.Final, bestOf: 5 },
      ],
    });

    const nodes = buildWizardTree(state);
    expect(nodes.some(n => n.label === 'Copa de Oro (Semifinal Bo3, Final Bo5)')).toBe(true);
  });

  it('includes the cross-division cup as a tagged node when enabled', () => {
    const state = makeValidState();
    const crossCup: CrossCupConfig = {
      ...state.crossCup,
      enabled: true,
      name: 'Copa Club12',
      includeAllTeams: true,
    };
    state.crossCup = crossCup;

    const nodes = buildWizardTree(state);
    const crossNode = nodes.find(n => n.id === 'cross-cup');
    expect(crossNode).toMatchObject({ depth: 2, tag: 'división cruzada' });
    expect(crossNode?.label).toContain('Copa Club12');
  });
});
