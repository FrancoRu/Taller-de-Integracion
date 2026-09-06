import { describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  IDivisionStructureResponse,
  ITournamentStructureResponse,
} from '@/modules/tournament/type/tournament.d';
import { ICreateFullTournamentRequest } from '@/modules/tournament/type/createFullTournament.d';
import { structureToWizardState } from './cloneWizard';
import { submitWizard, WizardServices } from './submitWizard';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const mapping = (fromPosition: number, toPosition: number, destination: string) => ({
  id: guid('mapping'),
  fromPosition,
  toPosition,
  destination,
});

const makeServices = (): WizardServices & Record<keyof WizardServices, ReturnType<typeof vi.fn>> =>
  ({
    createFullTournament: vi.fn(async () => (({
      id: guid('tournament'),
      slug: 'clon',
    }) as never)),
  }) as unknown as WizardServices & Record<keyof WizardServices, ReturnType<typeof vi.fn>>;

const payloadOf = (
  services: Record<keyof WizardServices, ReturnType<typeof vi.fn>>
): ICreateFullTournamentRequest =>
  services.createFullTournament.mock.calls[0][0] as ICreateFullTournamentRequest;

describe('structureToWizardState — golden round trips', () => {
  it('reconstructs a simple single-zone tournament (group + one cup) exactly', async () => {
    const dto: ITournamentStructureResponse = {
      name: 'Apertura 2026',
      description: 'Torneo de prueba',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona A',
          isCrossDivisionCup: false,
          pointsForWin: 3,
          pointsForLoss: 0,
          qualifiersPerGroup: 1,
          playoffMappings: [mapping(1, 4, 'Copa Oro')],
          stages: [
            { name: 'Fase de Grupos', bracketName: null, stageType: StageType.Group, isElimination: false, order: 0, bestOf: 1, roundRobinLegs: 2 },
            { name: 'Semifinal Copa Oro', bracketName: 'Copa Oro', stageType: StageType.SemiFinal, isElimination: true, order: 1, bestOf: 3, roundRobinLegs: 1 },
            { name: 'Tercer Puesto Copa Oro', bracketName: 'Copa Oro', stageType: StageType.ThirdPlace, isElimination: true, order: 2, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Final Copa Oro', bracketName: 'Copa Oro', stageType: StageType.Final, isElimination: true, order: 3, bestOf: 5, roundRobinLegs: 1 },
          ],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Feminine);
    expect(review).toEqual([]);
    expect(state.tournament.name).toBe('Apertura 2026 (copia)');
    // Category is the organizer's explicit choice, never the source's.
    expect(state.tournament.category).toBe(TournamentCategory.Feminine);
    expect(state.tournament.startDate).toBe('');
    expect(state.tournament.teamRegistrationDeadline).toBe('');

    const services = makeServices();
    await submitWizard(state, services);
    const division = payloadOf(services).divisions[0];

    expect(division).toMatchObject({
      name: 'Zona A',
      pointsForWin: 3,
      pointsForLoss: 0,
      playoffMappings: [{ fromPosition: 1, toPosition: 4, destination: 'Copa Oro' }],
    });
    const groupStage = division.stages.find(s => s.stageType === StageType.Group);
    expect(groupStage).toMatchObject({ roundRobinLegs: 2 });

    const cupStages = division.stages.filter(s => s.isElimination);
    expect(cupStages).toHaveLength(3);
    expect(cupStages[0]).toMatchObject({ stageType: StageType.SemiFinal, bestOf: 3 });
    expect(cupStages[1]).toMatchObject({ stageType: StageType.ThirdPlace, bestOf: 1 });
    expect(cupStages[2]).toMatchObject({ stageType: StageType.Final, bestOf: 5 });
  });

  it('reconstructs a zone split into M sub-groups exactly', async () => {
    const groupStage = (name: string): IDivisionStructureResponse['stages'][number] => ({
      name,
      bracketName: null,
      stageType: StageType.Group,
      isElimination: false,
      order: 0,
      bestOf: 1,
      roundRobinLegs: 2,
    });
    const dto: ITournamentStructureResponse = {
      name: 'Clausura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona B',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [],
          stages: [groupStage('Grupo A'), groupStage('Grupo B'), groupStage('Grupo C')],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Masculine);
    expect(review).toEqual([]);
    expect(state.zones[0]).toMatchObject({ hasGroupStage: true, subGroupCount: 3, roundRobinLegs: 2 });

    const services = makeServices();
    await submitWizard(state, services);
    const groupStages = payloadOf(services).divisions[0].stages.filter(
      s => s.stageType === StageType.Group
    );
    expect(groupStages.map(s => s.name)).toEqual(['Grupo A', 'Grupo B', 'Grupo C']);
  });

  it('reconstructs a cross-division cup exactly', async () => {
    const dto: ITournamentStructureResponse = {
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Copa Cruzada',
          isCrossDivisionCup: true,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 2,
          playoffMappings: [],
          stages: [
            { name: 'Grupo 1', bracketName: null, stageType: StageType.Group, isElimination: false, order: 0, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Grupo 2', bracketName: null, stageType: StageType.Group, isElimination: false, order: 1, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Semifinal Copa Cruzada', bracketName: 'Copa Cruzada', stageType: StageType.SemiFinal, isElimination: true, order: 2, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Tercer Puesto Copa Cruzada', bracketName: 'Copa Cruzada', stageType: StageType.ThirdPlace, isElimination: true, order: 3, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Final Copa Cruzada', bracketName: 'Copa Cruzada', stageType: StageType.Final, isElimination: true, order: 4, bestOf: 1, roundRobinLegs: 1 },
          ],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Masculine);
    expect(review).toEqual([]);
    expect(state.crossCup).toMatchObject({ enabled: true, groupCount: 2, qualifiersPerGroup: 2 });
    expect(state.crossCup.cups[0]).toMatchObject({ qualifiers: 4, hasThirdPlace: true });

    const services = makeServices();
    await submitWizard(state, services);
    const crossDivision = payloadOf(services).divisions.find(d => d.isCrossDivisionCup);
    expect(crossDivision).toMatchObject({ isCrossDivisionCup: true, qualifiersPerGroup: 2 });
    expect(crossDivision!.stages.filter(s => s.stageType === StageType.Group)).toHaveLength(2);
    expect(crossDivision!.stages.filter(s => s.isElimination)).toHaveLength(3);
  });

  it('reconstructs a playoffs-only (groupless) division exactly', async () => {
    const dto: ITournamentStructureResponse = {
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Reducido',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [mapping(1, 8, 'Copa Only')],
          stages: [
            { name: 'Cuartos Copa Only', bracketName: 'Copa Only', stageType: StageType.QuarterFinal, isElimination: true, order: 0, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Semifinal Copa Only', bracketName: 'Copa Only', stageType: StageType.SemiFinal, isElimination: true, order: 1, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Tercer Puesto Copa Only', bracketName: 'Copa Only', stageType: StageType.ThirdPlace, isElimination: true, order: 2, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Final Copa Only', bracketName: 'Copa Only', stageType: StageType.Final, isElimination: true, order: 3, bestOf: 1, roundRobinLegs: 1 },
          ],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Masculine);
    expect(review).toEqual([]);
    expect(state.zones[0]).toMatchObject({ hasGroupStage: false });
    expect(state.zones[0].cups[0]).toMatchObject({ qualifiers: 8 });

    const services = makeServices();
    await submitWizard(state, services);
    const division = payloadOf(services).divisions[0];
    expect(division.stages.filter(s => s.stageType === StageType.Group)).toHaveLength(0);
    expect(division.stages.filter(s => s.isElimination)).toHaveLength(4);
  });
});

describe('structureToWizardState — ambiguity is flagged, never silently guessed', () => {
  it('flags a zone whose sub-groups have inconsistent RoundRobinLegs and falls back to the minimum', () => {
    const dto: ITournamentStructureResponse = {
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona Rara',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [],
          stages: [
            { name: 'Grupo A', bracketName: null, stageType: StageType.Group, isElimination: false, order: 0, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Grupo B', bracketName: null, stageType: StageType.Group, isElimination: false, order: 1, bestOf: 1, roundRobinLegs: 2 },
          ],
        },
        {
          name: 'Zona Sana',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          playoffMappings: [],
          stages: [
            { name: 'Fase de Grupos', bracketName: null, stageType: StageType.Group, isElimination: false, order: 0, bestOf: 1, roundRobinLegs: 1 },
          ],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Masculine);

    expect(review).toHaveLength(1);
    expect(review[0]).toContain('Zona Rara');
    expect(state.zones[0].roundRobinLegs).toBe(1);
    // The other zone still pre-fills correctly.
    expect(state.zones[1]).toMatchObject({ name: 'Zona Sana', roundRobinLegs: 1 });
  });

  it('flags a cup whose PlayoffMapping destination matches no BracketName and falls back to the range minimum', () => {
    const dto: ITournamentStructureResponse = {
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona A',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          // Orphaned: destination does not match the real bracket's name below.
          playoffMappings: [mapping(1, 8, 'Copa Fantasma')],
          stages: [
            { name: 'Fase de Grupos', bracketName: null, stageType: StageType.Group, isElimination: false, order: 0, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Semifinal Copa Oro', bracketName: 'Copa Oro', stageType: StageType.SemiFinal, isElimination: true, order: 1, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Final Copa Oro', bracketName: 'Copa Oro', stageType: StageType.Final, isElimination: true, order: 2, bestOf: 1, roundRobinLegs: 1 },
          ],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Masculine);

    expect(review).toHaveLength(1);
    expect(review[0]).toContain('Copa Oro');
    // Semis+Final with no valid mapping falls back to the range minimum (3), not a guess.
    expect(state.zones[0].cups[0]).toMatchObject({ name: 'Copa Oro', qualifiers: 3 });
  });

  it('flags a mismatched mapping span against the actual bracket shape and falls back to the range minimum', () => {
    const dto: ITournamentStructureResponse = {
      name: 'Apertura 2026',
      category: TournamentCategory.Masculine,
      divisions: [
        {
          name: 'Zona A',
          isCrossDivisionCup: false,
          pointsForWin: 2,
          pointsForLoss: 1,
          qualifiersPerGroup: 1,
          // Mapping claims 8 qualifiers, but the bracket below is only Semis+Final (3-4 range) — a hand-edited mismatch.
          playoffMappings: [mapping(1, 8, 'Copa Oro')],
          stages: [
            { name: 'Fase de Grupos', bracketName: null, stageType: StageType.Group, isElimination: false, order: 0, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Semifinal Copa Oro', bracketName: 'Copa Oro', stageType: StageType.SemiFinal, isElimination: true, order: 1, bestOf: 1, roundRobinLegs: 1 },
            { name: 'Final Copa Oro', bracketName: 'Copa Oro', stageType: StageType.Final, isElimination: true, order: 2, bestOf: 1, roundRobinLegs: 1 },
          ],
        },
      ],
    };

    const { state, review } = structureToWizardState(dto, TournamentCategory.Masculine);

    expect(review).toHaveLength(1);
    expect(state.zones[0].cups[0]).toMatchObject({ qualifiers: 3 });
  });
});
