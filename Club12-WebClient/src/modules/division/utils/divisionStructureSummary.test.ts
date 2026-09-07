import { describe, expect, it } from 'vitest';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { ITournamentStructureResponse } from '@/modules/tournament/type/tournament.d';
import { findDivisionStructure } from './divisionStructureSummary';

const groupStage = (name: string): ITournamentStructureResponse['divisions'][number]['stages'][number] => ({
  name,
  bracketName: null,
  stageType: StageType.Group,
  isElimination: false,
  order: 0,
  bestOf: 1,
  roundRobinLegs: 2,
});

const cupStages = (bracketName: string) => [
  {
    name: `Semifinal ${bracketName}`,
    bracketName,
    stageType: StageType.SemiFinal,
    isElimination: true,
    order: 1,
    bestOf: 3,
    roundRobinLegs: 1,
  },
  {
    name: `Final ${bracketName}`,
    bracketName,
    stageType: StageType.Final,
    isElimination: true,
    order: 2,
    bestOf: 5,
    roundRobinLegs: 1,
  },
];

const tournamentStructure: ITournamentStructureResponse = {
  name: 'Apertura 2026',
  category: TournamentCategory.Masculine,
  divisions: [
    {
      name: 'Zona A',
      isCrossDivisionCup: false,
      pointsForWin: 3,
      pointsForLoss: 0,
      qualifiersPerGroup: 1,
      playoffMappings: [{ id: 'mapping-1', fromPosition: 1, toPosition: 4, destination: 'Copa Oro' } as never],
      stages: [groupStage('Fase de Grupos'), ...cupStages('Copa Oro')],
    },
    {
      name: 'Copa Club12',
      isCrossDivisionCup: true,
      pointsForWin: 2,
      pointsForLoss: 1,
      qualifiersPerGroup: 2,
      playoffMappings: [],
      stages: [groupStage('Grupo 1'), groupStage('Grupo 2'), ...cupStages('Copa Club12')],
    },
  ],
};

describe('findDivisionStructure', () => {
  it('resolves a regular zone by name', () => {
    const summary = findDivisionStructure(tournamentStructure, 'Zona A');

    expect(summary).not.toBeNull();
    expect(summary!.zone).toBeDefined();
    expect(summary!.crossCup).toBeUndefined();
    expect(summary!.zone!.name).toBe('Zona A');
    expect(summary!.zone!.cups).toHaveLength(1);
    expect(summary!.zone!.cups[0].name).toBe('Copa Oro');
    expect(summary!.review).toEqual([]);
  });

  it('resolves the cross-division cup by name', () => {
    const summary = findDivisionStructure(tournamentStructure, 'Copa Club12');

    expect(summary).not.toBeNull();
    expect(summary!.crossCup).toBeDefined();
    expect(summary!.zone).toBeUndefined();
    expect(summary!.crossCup!.groupCount).toBe(2);
    expect(summary!.crossCup!.cups[0].name).toBe('Copa Club12');
  });

  it('returns null when no division in the tournament matches the given name', () => {
    expect(findDivisionStructure(tournamentStructure, 'Zona Inexistente')).toBeNull();
  });
});
