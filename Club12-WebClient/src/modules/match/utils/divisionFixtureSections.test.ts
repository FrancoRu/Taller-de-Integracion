import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { buildDivisionFixtureSections } from '@/modules/match/utils/divisionFixtureSections';

const guid = (value: string) => value as GUID;

const team = (name: string): ITeamMatchResponse => ({
  id: guid(`team-${name}`),
  name,
  logoUrl: '',
  score: 0,
  players: [],
  scorers: [],
});

let sequence = 0;

const match = (stageId: string, overrides: Partial<IMatchResponse> = {}): IMatchResponse => ({
  id: guid(`match-${(sequence += 1)}`),
  matchDate: '2026-04-28T20:00:00Z',
  round: 1,
  matchType: MatchType.Regular,
  slug: `match-${sequence}`,
  homeTeam: team('A'),
  visitorTeam: team('B'),
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: guid(stageId),
  winningTeamName: null,
  status: null,
  ...overrides,
});

const stage = (
  overrides: Partial<Omit<IStageResponse, 'id'>> & { id: string; name: string }
): IStageResponse => ({
  slug: `stage-${overrides.id}`,
  description: null,
  stageType: StageType.Group,
  isActive: true,
  isElimination: false,
  startDate: '2026-04-01T00:00:00Z',
  endDate: '2026-05-01T00:00:00Z',
  divisionId: guid('division-1'),
  order: 0,
  bracketName: null,
  bestOf: 1,
  roundRobinLegs: 1,
  ...overrides,
  id: guid(overrides.id),
});

describe('buildDivisionFixtureSections', () => {
  it('labels a stage by its specific part, stripping the "{Division} - " prefix, ordered by order', () => {
    const stages = [
      stage({ id: 'final', name: 'Copa Club12 - Final', stageType: StageType.Final, order: 2 }),
      stage({ id: 'zona', name: 'Copa Club12 - ZONA 3', stageType: StageType.Group, order: 1 }),
    ];
    const matches = [match('zona'), match('final')];

    const sections = buildDivisionFixtureSections(stages, matches, 'Copa Club12');

    expect(sections.map(s => s.label)).toEqual(['ZONA 3', 'Final']);
    expect(sections[0].stage.id).toBe('zona');
    expect(sections[0].matches).toHaveLength(1);
    expect(sections[1].matches).toHaveLength(1);
  });

  it('labels each parallel group stage by its own name when there is more than one Group stage', () => {
    const stages = [
      stage({ id: 'g1', name: 'Grupo 1', stageType: StageType.Group, order: 1 }),
      stage({ id: 'g2', name: 'Grupo 2', stageType: StageType.Group, order: 2 }),
    ];
    const matches = [match('g1'), match('g2')];

    const sections = buildDivisionFixtureSections(stages, matches, 'Copa Club12');

    expect(sections.map(s => s.label)).toEqual(['Grupo 1', 'Grupo 2']);
  });

  it('drops stages that have no matches (empty-section filtering)', () => {
    const stages = [
      stage({ id: 'played', name: 'Copa Club12 - ZONA 1', stageType: StageType.Group, order: 1 }),
      stage({ id: 'empty', name: 'Copa Club12 - ZONA 2', stageType: StageType.Group, order: 2 }),
    ];
    const matches = [match('played')];

    const sections = buildDivisionFixtureSections(stages, matches, 'Copa Club12');

    expect(sections).toHaveLength(1);
    expect(sections[0].stage.id).toBe('played');
  });
});
