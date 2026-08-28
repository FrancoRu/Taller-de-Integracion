import { describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
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
          rounds: [
            { id: 'r1', stageType: StageType.SemiFinal, bestOf: 3 },
            { id: 'r2', stageType: StageType.Final, bestOf: 5 },
          ],
        },
      ],
      pointsForWin: 3,
      pointsForLoss: 0,
      playoffMappings: [
        { id: 'm1', fromPosition: 1, toPosition: 2, destination: 'Copa de Oro' },
      ],
    },
  ];
  return state;
};

const makeServices = (
  overrides: Partial<WizardServices> = {}
): WizardServices & Record<keyof WizardServices, ReturnType<typeof vi.fn>> => ({
  addTournament: vi.fn(async () => (({
    id: guid('tournament')
  }) as never)),
  addDivision: vi.fn(async ({ name, tournamentId, isCrossDivisionCup }: never) => (({
    id: guid('division'),
    name,
    tournamentId,
    isCrossDivisionCup
  }) as never)),
  addStage: vi.fn(async () => (({
    id: guid('stage')
  }) as never)),
  ...overrides,
}) as unknown as WizardServices & Record<keyof WizardServices, ReturnType<typeof vi.fn>>;

describe('submitWizard', () => {
  it('creates the tournament as structure only and reports success', async () => {
    const services = makeServices();
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(true);
    expect(result.tournamentId).toBe(guid('tournament'));
    expect(result.warnings).toEqual([]);
    expect(services.addTournament).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'Apertura 2026' })
    );
    // minTeams/maxTeams were dropped from the backend contract; the wizard
    // must not send them.
    const addTournamentArg = services.addTournament.mock.calls[0][0];
    expect(addTournamentArg).not.toHaveProperty('minTeams');
    expect(addTournamentArg).not.toHaveProperty('maxTeams');
  });

  /**
   * HU-106: the wizard now creates ONLY the tournament + its
   * division/zone/cup/stage STRUCTURE. Teams are added later (registration
   * phase) and assigned to divisions only after registration closes
   * (HU-107/108). The wizard must therefore never register teams, assign
   * teams to a stage, nor generate any fixture — the tournament is left in
   * OpenForRegistration with no matches.
   */
  it('creates only structure — never registers teams, assigns teams, or generates a fixture', async () => {
    const registerTeams = vi.fn(async () => true);
    const assignTeamsToStage = vi.fn(async () => true);
    const generateMatches = vi.fn(async () => true);

    const services = {
      ...makeServices(),
      // These must NOT exist on the contract anymore, but even if a caller
      // still injects them, submitWizard must never invoke them.
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

  it('creates one division per zone with isCrossDivisionCup false', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    expect(services.addDivision).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'Zona A', tournamentId: guid('tournament'), isCrossDivisionCup: false })
    );
  });

  it('sends the per-division points (HU-79) and playoff range mappings (HU-45) on addDivision', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    expect(services.addDivision).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Zona A',
        pointsForWin: 3,
        pointsForLoss: 0,
        playoffMappings: [{ fromPosition: 1, toPosition: 2, destination: 'Copa de Oro' }],
      })
    );

    // The local React `id` is stripped from every mapping before it is sent.
    const divisionArg = services.addDivision.mock.calls[0][0] as {
      playoffMappings: Array<Record<string, unknown>>;
    };
    expect(divisionArg.playoffMappings[0]).not.toHaveProperty('id');
  });

  it('drops half-filled playoff ranges (no destination cup chosen) before sending', async () => {
    const services = makeServices();
    const state = makeState();
    state.zones[0].playoffMappings = [
      { id: 'm1', fromPosition: 1, toPosition: 2, destination: 'Copa de Oro' },
      { id: 'm2', fromPosition: 3, toPosition: 4, destination: '   ' },
    ];

    await submitWizard(state, services);

    const divisionArg = services.addDivision.mock.calls[0][0] as {
      playoffMappings: Array<Record<string, unknown>>;
    };
    expect(divisionArg.playoffMappings).toEqual([
      { fromPosition: 1, toPosition: 2, destination: 'Copa de Oro' },
    ]);
  });

  it('creates the group stage as structure with the configured RoundRobinLegs and no team assignment', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    expect(services.addStage).toHaveBeenCalledWith(
      expect.objectContaining({ stageType: StageType.Group, isElimination: false, roundRobinLegs: 2 })
    );
  });

  it('creates one stage per cup round with the bracket name and best-of', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    const cupStageCalls = services.addStage.mock.calls.filter(
      call => (call[0] as { isElimination: boolean }).isElimination
    );
    expect(cupStageCalls).toHaveLength(2);
    expect(cupStageCalls[0][0]).toMatchObject({
      stageType: StageType.SemiFinal,
      bracketName: 'Copa de Oro',
      bestOf: 3,
    });
    expect(cupStageCalls[1][0]).toMatchObject({
      stageType: StageType.Final,
      bracketName: 'Copa de Oro',
      bestOf: 5,
    });
  });

  it('aborts immediately and reports an error when tournament creation fails', async () => {
    const services = makeServices({ addTournament: vi.fn(async () => undefined) });
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(false);
    expect(result.error).toBeDefined();
    expect(services.addDivision).not.toHaveBeenCalled();
  });

  it('records a warning and skips stage creation when a zone division fails to create', async () => {
    const services = makeServices({ addDivision: vi.fn(async () => undefined) });
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(true);
    expect(result.warnings.some(w => w.includes('Zona A'))).toBe(true);
    expect(services.addStage).not.toHaveBeenCalled();
  });

  it('creates a cross-division cup division with isCrossDivisionCup true when enabled', async () => {
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
      playoffMappings: [],
    };

    await submitWizard(state, services);

    expect(services.addDivision).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'Copa Club12', isCrossDivisionCup: true })
    );
  });

  it('does not create a cross-division cup division when disabled', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    expect(services.addDivision).toHaveBeenCalledTimes(1);
  });

  // HU-110: the cross cup is a multi-group competition. The wizard must create
  // ONE Group stage per configured group ("Grupo 1"…"Grupo N"), each with the
  // configured RoundRobinLegs, and must never send a match count — the bracket
  // is auto-sized by the backend from the pooled qualifiers.
  it('creates one Group stage per configured cross-cup group with the configured RoundRobinLegs', async () => {
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
      playoffMappings: [],
    };

    await submitWizard(state, services);

    const crossGroupCalls = services.addStage.mock.calls.filter(call => {
      const stage = call[0] as { stageType: StageType; name: string };
      return stage.stageType === StageType.Group && stage.name.startsWith('Grupo ');
    });

    expect(crossGroupCalls).toHaveLength(3);
    expect(crossGroupCalls.map(call => (call[0] as { name: string }).name)).toEqual([
      'Grupo 1',
      'Grupo 2',
      'Grupo 3',
    ]);
    for (const call of crossGroupCalls) {
      expect(call[0]).toMatchObject({
        stageType: StageType.Group,
        isElimination: false,
        roundRobinLegs: 2,
      });
      // The wizard never sends a match count for the cross-cup groups.
      expect(call[0]).not.toHaveProperty('numberOfMatches');
      expect(call[0]).not.toHaveProperty('matchCount');
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
      playoffMappings: [],
    };

    await submitWizard(state, services);

    expect(services.addDivision).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Copa Club12',
        isCrossDivisionCup: true,
        qualifiersPerGroup: 4,
      })
    );

    // The regular zone must not carry qualifiersPerGroup — it is a cross-cup
    // only concept.
    const zoneCall = services.addDivision.mock.calls.find(
      call => (call[0] as { name: string }).name === 'Zona A'
    );
    expect(zoneCall?.[0]).not.toHaveProperty('qualifiersPerGroup');
  });

  // HU-48: the chosen category is set at creation on the tournament and must
  // be echoed onto every division, because the backend rejects a division
  // whose category differs from its tournament (Division.Category defaults to
  // Masculine server-side).
  it('sends the tournament category on addTournament', async () => {
    const services = makeServices();
    const state = makeState();
    state.tournament.category = TournamentCategory.Feminine;

    await submitWizard(state, services);

    expect(services.addTournament).toHaveBeenCalledWith(
      expect.objectContaining({ category: TournamentCategory.Feminine })
    );
  });

  it('sends the same category on every division-create call (zones and cross-cup)', async () => {
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
      playoffMappings: [],
    };

    await submitWizard(state, services);

    expect(services.addDivision).toHaveBeenCalledTimes(2);
    for (const call of services.addDivision.mock.calls) {
      expect(call[0]).toEqual(
        expect.objectContaining({ category: TournamentCategory.Feminine })
      );
    }
  });
});
