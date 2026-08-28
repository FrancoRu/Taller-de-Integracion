import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  CrossCupConfig,
  CupConfig,
  PlayoffMappingConfig,
  WizardState,
  ZoneConfig,
  createInitialWizardState,
} from './types';
import {
  buildWizardTree,
  isWizardReadyToSubmit,
  resolveCrossCupTeamIds,
  validateCrossCupStep,
  validatePlayoffMappings,
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
    category: TournamentCategory.Masculine,
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
      pointsForWin: 2,
      pointsForLoss: 1,
      playoffMappings: [],
    },
    {
      id: 'zone-2',
      name: 'Zona B',
      teamIds: [guid('c'), guid('d')],
      hasGroupStage: true,
      roundRobinLegs: 1,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
      playoffMappings: [],
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
});

describe('validateTeamsStep', () => {
  it('rejects a tournament with no teams inscribed', () => {
    const state = makeValidState();
    state.selectedTeamIds = [];
    expect(validateTeamsStep(state).length).toBeGreaterThan(0);
  });

  it('accepts at least one inscribed team', () => {
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
      pointsForWin: 2,
      pointsForLoss: 1,
      playoffMappings: [],
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

describe('validatePlayoffMappings', () => {
  const mapping = (
    fromPosition: number,
    toPosition: number,
    destination: string
  ): PlayoffMappingConfig => ({ id: `${fromPosition}-${toPosition}`, fromPosition, toPosition, destination });

  const cups = ['Copa Oro', 'Copa Plata'];

  it('accepts a clean, non-overlapping partition within the team count', () => {
    const mappings = [mapping(1, 4, 'Copa Oro'), mapping(5, 8, 'Copa Plata')];
    expect(validatePlayoffMappings(mappings, 8, cups, 'la zona')).toEqual([]);
  });

  it('is a no-op when there are no mappings', () => {
    expect(validatePlayoffMappings([], 8, cups, 'la zona')).toEqual([]);
  });

  it('rejects two ranges that overlap', () => {
    const mappings = [mapping(1, 4, 'Copa Oro'), mapping(4, 8, 'Copa Plata')];
    expect(validatePlayoffMappings(mappings, 8, cups, 'la zona').some(e => e.includes('solapan'))).toBe(
      true
    );
  });

  it('rejects overlap regardless of the order the rows were entered', () => {
    const mappings = [mapping(5, 8, 'Copa Plata'), mapping(3, 6, 'Copa Oro')];
    expect(validatePlayoffMappings(mappings, 8, cups, 'la zona').some(e => e.includes('solapan'))).toBe(
      true
    );
  });

  it('rejects a range that exceeds the team count', () => {
    const mappings = [mapping(1, 12, 'Copa Oro')];
    expect(validatePlayoffMappings(mappings, 8, cups, 'la zona').some(e => e.includes('supera'))).toBe(
      true
    );
  });

  it('skips the upper-bound check when no teams are assigned yet', () => {
    const mappings = [mapping(1, 12, 'Copa Oro')];
    expect(validatePlayoffMappings(mappings, 0, cups, 'la zona')).toEqual([]);
  });

  it('rejects an inverted range (from greater than to)', () => {
    const mappings = [mapping(5, 2, 'Copa Oro')];
    expect(
      validatePlayoffMappings(mappings, 8, cups, 'la zona').some(e => e.includes('invertido'))
    ).toBe(true);
  });

  it('rejects a destination that is not one of the configured cups', () => {
    const mappings = [mapping(1, 4, 'Copa Bronce')];
    expect(
      validatePlayoffMappings(mappings, 8, cups, 'la zona').some(e => e.includes('no coincide'))
    ).toBe(true);
  });

  it('rejects a mapping with no destination chosen', () => {
    const mappings = [mapping(1, 4, '')];
    expect(
      validatePlayoffMappings(mappings, 8, cups, 'la zona').some(e => e.includes('copa de destino'))
    ).toBe(true);
  });
});

describe('validateZonesStep with playoff mappings', () => {
  it('surfaces an overlap error from a zone\'s playoff mappings', () => {
    const state = makeValidState();
    state.zones[0].cups = [
      { id: 'cup-1', name: 'Copa Oro', rounds: [{ id: 'r1', stageType: StageType.Final, bestOf: 1 }] },
    ];
    state.zones[0].playoffMappings = [
      { id: 'm1', fromPosition: 1, toPosition: 2, destination: 'Copa Oro' },
      { id: 'm2', fromPosition: 2, toPosition: 2, destination: 'Copa Oro' },
    ];
    expect(validateZonesStep(state).some(e => e.includes('solapan'))).toBe(true);
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
