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
  state.selectedTeamIds = [guid('a'), guid('b')];
  state.zones = [
    {
      id: 'zone-1',
      name: 'Zona A',
      teamIds: [guid('a'), guid('b')],
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
  registerTeams: vi.fn(async () => true),
  addDivision: vi.fn(async ({ name, tournamentId, isCrossDivisionCup }: never) => (({
    id: guid('division'),
    name,
    tournamentId,
    isCrossDivisionCup
  }) as never)),
  addStage: vi.fn(async () => (({
    id: guid('stage')
  }) as never)),
  assignTeamsToStage: vi.fn(async () => true),
  generateMatches: vi.fn(async () => true),
  ...overrides,
}) as unknown as WizardServices & Record<keyof WizardServices, ReturnType<typeof vi.fn>>;

describe('submitWizard', () => {
  it('creates the tournament, registers teams, and reports success', async () => {
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
    expect(services.registerTeams).toHaveBeenCalledWith(guid('tournament'), [guid('a'), guid('b')]);
  });

  /**
   * Regression test for a real bug found by driving the actual wizard
   * against a live backend: the server infers an unassigned stage's
   * round-robin pool from "how many teams are currently registered to the
   * tournament" when it has no better signal. Registering every zone's
   * teams in one batch up front (before any zone-specific assignment)
   * meant that signal was always the TOURNAMENT-WIDE team count, never a
   * single zone's own count, so every zone but the last ended up with the
   * wrong fixture. Registering only each zone's own teams right before its
   * own assignment/generation step keeps that signal correct per zone; the
   * final call restores everyone's registration once every fixture exists.
   */
  it('registers only each zone\'s own teams before assigning/generating its fixture, then registers everyone at the end', async () => {
    const registerCalls: unknown[][] = [];
    const services = makeServices({
      registerTeams: vi.fn(async (...args: unknown[]) => {
        registerCalls.push(args);
        return true;
      }),
    });

    const state = makeState();
    state.selectedTeamIds = [guid('a'), guid('b'), guid('c'), guid('d')];
    state.zones = [
      { ...state.zones[0], teamIds: [guid('a'), guid('b')] },
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

    await submitWizard(state, services);

    expect(registerCalls).toEqual([
      [guid('tournament'), [guid('a'), guid('b')]],
      [guid('tournament'), [guid('c'), guid('d')]],
      [guid('tournament'), [guid('a'), guid('b'), guid('c'), guid('d')]],
    ]);
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

  it('creates the group stage with the configured RoundRobinLegs and assigns the zone teams to it', async () => {
    const services = makeServices();
    await submitWizard(makeState(), services);

    expect(services.addStage).toHaveBeenCalledWith(
      expect.objectContaining({ stageType: StageType.Group, isElimination: false, roundRobinLegs: 2 })
    );
    expect(services.assignTeamsToStage).toHaveBeenCalledWith(guid('stage'), [guid('a'), guid('b')], false);
    expect(services.generateMatches).toHaveBeenCalledWith(guid('stage'));
  });

  it('creates one stage per cup round with the bracket name and best-of, but never assigns teams to them', async () => {
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

    // assignTeamsToStage was called exactly once — for the group stage, not the cup rounds.
    expect(services.assignTeamsToStage).toHaveBeenCalledTimes(1);
  });

  it('aborts immediately and reports an error when tournament creation fails', async () => {
    const services = makeServices({ addTournament: vi.fn(async () => undefined) });
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(false);
    expect(result.error).toBeDefined();
    expect(services.addDivision).not.toHaveBeenCalled();
  });

  it('keeps going and records a warning when team registration fails', async () => {
    const services = makeServices({ registerTeams: vi.fn(async () => undefined) });
    const result = await submitWizard(makeState(), services);

    expect(result.success).toBe(true);
    expect(result.warnings.some(w => w.includes('registrar'))).toBe(true);
    expect(services.addDivision).toHaveBeenCalled();
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
      includeAllTeams: true,
      teamIds: [],
      hasGroupStage: false,
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
      includeAllTeams: true,
      teamIds: [],
      hasGroupStage: false,
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
