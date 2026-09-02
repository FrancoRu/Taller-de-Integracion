import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse, ISeriesGameResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { groupMatchesBySeries } from '@/modules/matchSeries/utils/groupMatchesBySeries';

const guid = (value: string) => value as GUID;

const match = (id: string): IMatchResponse => ({
  id: guid(id),
  matchDate: '2026-05-01T00:00:00Z',
  matchType: MatchType.Playoff,
  slug: id,
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  stageId: guid('stage-1'),
});

const game = (id: string, gameNumber: number): ISeriesGameResponse => ({
  id: guid(id),
  matchDate: '2026-05-01T00:00:00Z',
  homeTeamName: 'Home',
  visitorTeamName: 'Visitor',
  homeScore: null,
  visitorScore: null,
  winningTeamName: null,
  isFinished: false,
  matchType: MatchType.Playoff,
  gameNumber,
});

const series = (id: string, gameIds: string[]): IMatchSeriesResponse => ({
  id: guid(id),
  stageId: guid('stage-1'),
  homeTeamId: guid('home'),
  homeTeamName: 'Home',
  visitorTeamId: guid('visitor'),
  visitorTeamName: 'Visitor',
  bestOf: 3,
  winningTeamId: null,
  winningTeamName: null,
  games: gameIds.map((gameId, index) => game(gameId, index + 1)),
});

describe('groupMatchesBySeries', () => {
  it('groups every game of a series into one entry, in the order its first game appears', () => {
    const g1 = match('g1');
    const g2 = match('g2');
    const s = series('series-1', ['g1', 'g2']);

    const groups = groupMatchesBySeries([g1, g2], new Map([[s.id, s]]));

    expect(groups).toHaveLength(1);
    expect(groups[0].series?.id).toBe(s.id);
    expect(groups[0].matches).toEqual([g1, g2]);
  });

  it('keeps a standalone match (no series) as its own single-match group', () => {
    const solo = match('solo');

    const groups = groupMatchesBySeries([solo], new Map());

    expect(groups).toEqual([{ series: null, matches: [solo] }]);
  });

  it('keeps two interleaved series separate, each with only its own games', () => {
    const a1 = match('a1');
    const b1 = match('b1');
    const a2 = match('a2');
    const seriesA = series('series-a', ['a1', 'a2']);
    const seriesB = series('series-b', ['b1']);

    const groups = groupMatchesBySeries(
      [a1, b1, a2],
      new Map([
        [seriesA.id, seriesA],
        [seriesB.id, seriesB],
      ])
    );

    expect(groups).toHaveLength(2);
    expect(groups[0].series?.id).toBe(seriesA.id);
    expect(groups[0].matches).toEqual([a1, a2]);
    expect(groups[1].series?.id).toBe(seriesB.id);
    expect(groups[1].matches).toEqual([b1]);
  });

  it('preserves overall input order across a mix of series and standalone matches', () => {
    const solo1 = match('solo1');
    const a1 = match('a1');
    const solo2 = match('solo2');
    const seriesA = series('series-a', ['a1']);

    const groups = groupMatchesBySeries([solo1, a1, solo2], new Map([[seriesA.id, seriesA]]));

    expect(groups.map(g => g.matches[0].id)).toEqual([solo1.id, a1.id, solo2.id]);
  });
});
