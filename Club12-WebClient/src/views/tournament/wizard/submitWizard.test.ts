import { describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { ICreateFullTournamentRequest } from '@/modules/tournament/type/createFullTournament.d';
import { WizardState, createInitialWizardState } from './types';
import { WizardServices, submitWizard } from './submitWizard';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const makeState = (): WizardState => {
  const state = createInitialWizardState();
  state.tournament = {
    name: 'Apertura 2026',
    description: 'Torneo de prueba',
    startDate: '2026-03-01',
    teamRegistrationDeadline: '2026-02-15',
    category: TournamentCategory.Masculine,
    seasonId: guid('season'),
  };
  state.zones = [
    {
      id: 'zone-1',
      name: 'Zona A',
      hasGroupStage: true,
      roundRobinLegs: 2,
      cups: [
        {
          id: 'cup-1',
          name: 'Copa de Oro',
          qualifiers: 4,
          bestOfByStage: {},
        },
      ],
      pointsForWin: 3,
      pointsForLoss: 0,
    },
  ];
  return state;
};

const makeServices = (
  overrides: Partial<WizardServices> = {}
): WizardServices & Record<keyof WizardServices, ReturnType<typeof vi.fn>> => ({
  createFullTournament: vi.fn(async () => (({
    id: guid('tournament'),
    slug: 'apertura-2026',
  }) as never)),
  ...overrides,
}) as unknown as WizardServices &
  Record<keyof WizardServices, ReturnType<typeof vi.fn>>;

/** Pulls the single payload the wizard POSTed to /full. */
const payloadOf = (
  services: Record<keyof WizardServices, ReturnType<typeof vi.fn>>
): ICreateFullTournamentRequest =>
  services.createFullTournament.mock.calls[0][0] as ICreateFullTournamentRequest;

describe('submitWizard', () => {
  it('issues exactly ONE createFullTournament call with the fully-nested payload', async () => {
    const services = makeServices();
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(true);
    expect(result.tournamentId).toBe(guid('tournament'));
    expect(result.slug).toBe('apertura-2026');

    // A single atomic call — no per-division/per-stage/open-registration calls.
    expect(services.createFullTournament).toHaveBeenCalledTimes(1);

    const payload = payloadOf(services);
    expect(payload).toMatchObject({
      name: 'Apertura 2026',
      description: 'Torneo de prueba',
      category: TournamentCategory.Masculine,
    });
    // The whole structure is nested under the one payload.
    expect(payload.divisions).toHaveLength(1);
    expect(payload.divisions[0].stages.length).toBeGreaterThan(0);

    // minTeams/maxTeams were dropped from the backend contract; the wizard
    // must not send them.
    expect(payload).not.toHaveProperty('minTeams');
    expect(payload).not.toHaveProperty('maxTeams');
  });

  it('sends startDate and teamRegistrationDeadline as Date values', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    const payload = payloadOf(services);
    expect(payload.startDate).toBeInstanceOf(Date);
    expect(payload.teamRegistrationDeadline).toBeInstanceOf(Date);
  });

  /**
   * HU-106: the wizard creates ONLY the tournament + its
   * division/zone/cup/stage STRUCTURE. Teams are added later (registration
   * phase) and assigned to divisions only after registration closes
   * (HU-107/108). The single /full call is the only backend interaction — the
   * wizard never registers teams, assigns teams, nor generates any fixture.
   */
  it('creates only structure — never registers teams, assigns teams, or generates a fixture', async () => {
    const registerTeams = vi.fn(async () => true);
    const assignTeamsToStage = vi.fn(async () => true);
    const generateMatches = vi.fn(async () => true);

    const services = {
      ...makeServices(),
      registerTeams,
      assignTeamsToStage,
      generateMatches,
    } as unknown as WizardServices;

    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(true);
    expect(registerTeams).not.toHaveBeenCalled();
    expect(assignTeamsToStage).not.toHaveBeenCalled();
    expect(generateMatches).not.toHaveBeenCalled();
  });

  it('maps each zone to one division with isCrossDivisionCup false', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    const payload = payloadOf(services);
    expect(payload.divisions[0]).toMatchObject({
      name: 'Zona A',
      isCrossDivisionCup: false,
    });
  });

  it('HU-112: derives the per-division points (HU-79) and playoff ranges from the cups', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    // The single cup "Copa de Oro" has qualifiers=4, so the derived range is
    // positions 1-4 (no manual range editor anymore).
    expect(payloadOf(services).divisions[0]).toMatchObject({
      name: 'Zona A',
      pointsForWin: 3,
      pointsForLoss: 0,
      playoffMappings: [
        { fromPosition: 1, toPosition: 4, destination: 'Copa de Oro' },
      ],
    });
  });

  it('HU-112: derives top-down position ranges from the cups order and qualifiers', async () => {
    const services = makeServices();
    const state = makeState();
    state.zones[0].cups = [
      { id: 'cup-oro', name: 'Copa de Oro', qualifiers: 4, bestOfByStage: {} },
      { id: 'cup-plata', name: 'Copa de Plata', qualifiers: 4, bestOfByStage: {} },
    ];

    await submitWizard(state, services);

    const mappings = payloadOf(services).divisions[0].playoffMappings;
    expect(mappings).toEqual([
      { fromPosition: 1, toPosition: 4, destination: 'Copa de Oro' },
      { fromPosition: 5, toPosition: 8, destination: 'Copa de Plata' },
    ]);
    expect(mappings?.[0]).not.toHaveProperty('id');
  });

  it('nests the group stage with the configured RoundRobinLegs and no team assignment', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    const stages = payloadOf(services).divisions[0].stages;
    const groupStage = stages.find(s => s.stageType === StageType.Group);
    expect(groupStage).toMatchObject({
      stageType: StageType.Group,
      isElimination: false,
      roundRobinLegs: 2,
    });
  });

  it('HU-112: derives the cup rounds from the qualifier count (4 -> Semis + Final)', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    const cupStages = payloadOf(services).divisions[0].stages.filter(
      s => s.isElimination
    );
    // qualifiers=4 derives exactly Semifinal + Final, both using the cup's bestOf (3).
    expect(cupStages).toHaveLength(2);
    expect(cupStages[0]).toMatchObject({
      stageType: StageType.SemiFinal,
      bracketName: 'Copa de Oro',
      bestOf: 3,
    });
    expect(cupStages[1]).toMatchObject({
      stageType: StageType.Final,
      bracketName: 'Copa de Oro',
      bestOf: 3,
    });
  });

  it('reports an error and does not report success when the create fails', async () => {
    const services = makeServices({
      createFullTournament: vi.fn(async () => undefined),
    });
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(false);
    expect(result.error).toBeDefined();
    expect(result.tournamentId).toBeUndefined();
  });

  it('nests a cross-division cup division with isCrossDivisionCup true when enabled', async () => {
    const services = makeServices();
    const state = makeState();
    state.crossCup = {
      enabled: true,
      name: 'Copa Club12',
      groupCount: 1,
      qualifiersPerGroup: 1,
      roundRobinLegs: 1,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
    };

    await submitWizard(state, services);

    const payload = payloadOf(services);
    expect(payload.divisions).toHaveLength(2);
    expect(payload.divisions[1]).toMatchObject({
      name: 'Copa Club12',
      isCrossDivisionCup: true,
    });
  });

  it('does not nest a cross-division cup division when disabled', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    expect(payloadOf(services).divisions).toHaveLength(1);
  });

  // HU-110: the cross cup is a multi-group competition. The wizard must nest
  // ONE Group stage per configured group ("Grupo 1"…"Grupo N"), each with the
  // configured RoundRobinLegs, and must never send a match count — the bracket
  // is auto-sized by the backend from the pooled qualifiers.
  it('nests one Group stage per configured cross-cup group with the configured RoundRobinLegs', async () => {
    const services = makeServices();
    const state = makeState();
    state.crossCup = {
      enabled: true,
      name: 'Copa Club12',
      groupCount: 3,
      qualifiersPerGroup: 2,
      roundRobinLegs: 2,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
    };

    await submitWizard(state, services);

    const crossDivision = payloadOf(services).divisions[1];
    const crossGroups = crossDivision.stages.filter(
      s => s.stageType === StageType.Group && s.name.startsWith('Grupo ')
    );

    expect(crossGroups).toHaveLength(3);
    expect(crossGroups.map(s => s.name)).toEqual([
      'Grupo 1',
      'Grupo 2',
      'Grupo 3',
    ]);
    for (const stage of crossGroups) {
      expect(stage).toMatchObject({
        stageType: StageType.Group,
        isElimination: false,
        roundRobinLegs: 2,
      });
      expect(stage).not.toHaveProperty('numberOfMatches');
      expect(stage).not.toHaveProperty('matchCount');
    }
  });

  it('sends qualifiersPerGroup on the cross-cup division and omits it on regular zones', async () => {
    const services = makeServices();
    const state = makeState();
    state.crossCup = {
      enabled: true,
      name: 'Copa Club12',
      groupCount: 2,
      qualifiersPerGroup: 4,
      roundRobinLegs: 1,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
    };

    await submitWizard(state, services);

    const payload = payloadOf(services);
    const crossDivision = payload.divisions.find(d => d.name === 'Copa Club12');
    expect(crossDivision).toMatchObject({
      isCrossDivisionCup: true,
      qualifiersPerGroup: 4,
    });

    // The regular zone must not carry qualifiersPerGroup — it is a cross-cup
    // only concept.
    const zone = payload.divisions.find(d => d.name === 'Zona A');
    expect(zone).not.toHaveProperty('qualifiersPerGroup');
  });

  // HU-48: the chosen category is set at creation on the tournament and must
  // be echoed onto every division, because the backend rejects a division
  // whose category differs from its tournament.
  it('sends the tournament category on the payload', async () => {
    const services = makeServices();
    const state = makeState();
    state.tournament.category = TournamentCategory.Feminine;

    await submitWizard(state, services);

    expect(payloadOf(services).category).toBe(TournamentCategory.Feminine);
  });

  it('echoes the same category on every division (zones and cross-cup)', async () => {
    const services = makeServices();
    const state = makeState();
    state.tournament.category = TournamentCategory.Feminine;
    state.crossCup = {
      enabled: true,
      name: 'Copa Club12',
      groupCount: 1,
      qualifiersPerGroup: 1,
      roundRobinLegs: 1,
      cups: [],
      pointsForWin: 2,
      pointsForLoss: 1,
    };

    await submitWizard(state, services);

    const payload = payloadOf(services);
    expect(payload.divisions).toHaveLength(2);
    for (const division of payload.divisions) {
      expect(division.category).toBe(TournamentCategory.Feminine);
    }
  });

  // HU-38: the /full endpoint persists SeasonId. The wizard always carries the
  // RESOLVED season GUID in state.tournament.seasonId — whether seeded from the
  // season-hub launch (preset, locked select) or chosen manually in the
  // Temporada select — so the payload must include it so the created tournament
  // is grouped under its season.
  it('includes the seasonId from the wizard state in the payload (preset season-hub launch)', async () => {
    const services = makeServices();
    const state = makeState();
    // The season hub seeds the resolved GUID into state.tournament.seasonId.
    state.tournament.seasonId = guid('preset-season');

    await submitWizard(state, services);

    expect(payloadOf(services).seasonId).toBe(guid('preset-season'));
  });

  it('includes the seasonId from the wizard state in the payload (manual selection)', async () => {
    const services = makeServices();
    const state = makeState();
    // A manual Temporada selection sets the same field to the chosen GUID.
    state.tournament.seasonId = guid('manual-season');

    await submitWizard(state, services);

    expect(payloadOf(services).seasonId).toBe(guid('manual-season'));
  });

  it('omits seasonId when the wizard state carries none', async () => {
    const services = makeServices();
    const state = makeState();
    state.tournament.seasonId = '';

    await submitWizard(state, services);

    expect(payloadOf(services)).not.toHaveProperty('seasonId');
  });
});
