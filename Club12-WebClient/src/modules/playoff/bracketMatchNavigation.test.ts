import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse, ISeriesGameResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { resolveClickTargetMatchId } from './bracketMatchNavigation';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const baseMatch = (overrides: Partial<IMatchResponse> = {}): IMatchResponse => ({
  id: guid('match'),
  matchDate: '2026-01-01T18:00:00Z',
  matchType: 'Playoff' as IMatchResponse['matchType'],
  slug: '',
  homeTeam: { id: guid('home'), name: 'Home', logoUrl: '', score: 0, players: [], scorers: [] },
  visitorTeam: { id: guid('visitor'), name: 'Visitor', logoUrl: '', score: 0, players: [], scorers: [] },
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  stageId: guid('stage'),
  ...overrides,
});

const makeGame = (overrides: Partial<ISeriesGameResponse> & { id: GUID }): ISeriesGameResponse => ({
  matchDate: '2026-01-01T18:00:00Z',
  homeTeamName: 'Home',
  visitorTeamName: 'Visitor',
  homeScore: null,
  visitorScore: null,
  winningTeamName: null,
  isFinished: false,
  matchType: 'Playoff' as ISeriesGameResponse['matchType'],
  gameNumber: 1,
  ...overrides,
});

const baseSeries = (games: ISeriesGameResponse[]): IMatchSeriesResponse => ({
  id: guid('series'),
  stageId: guid('stage'),
  homeTeamId: guid('home'),
  homeTeamName: 'Home',
  visitorTeamId: guid('visitor'),
  visitorTeamName: 'Visitor',
  bestOf: 3,
  winningTeamId: null,
  winningTeamName: null,
  games,
});

describe('resolveClickTargetMatchId', () => {
  it('returns the plain match id when there is no series and no legs', () => {
    const match = baseMatch();
    expect(resolveClickTargetMatchId(match, undefined, undefined)).toBe(match.id);
  });

  it('returns undefined when a side is still TBD, even for a plain match', () => {
    const match = baseMatch({ homeTeam: null });
    expect(resolveClickTargetMatchId(match, undefined, undefined)).toBeUndefined();
  });

  it('never returns the synthetic series id — resolves to the first unfinished game', () => {
    const match = baseMatch({ id: guid('series') });
    const g1 = makeGame({ id: guid('g1'), gameNumber: 1, isFinished: true });
    const g2 = makeGame({ id: guid('g2'), gameNumber: 2, isFinished: false });
    const series = baseSeries([g1, g2]);

    const targetId = resolveClickTargetMatchId(match, series, undefined);

    expect(targetId).toBe(g2.id);
    expect(targetId).not.toBe(match.id);
  });

  it('resolves to the last game once every game in the series is finished', () => {
    const match = baseMatch({ id: guid('series') });
    const g1 = makeGame({ id: guid('g1'), gameNumber: 1, isFinished: true });
    const g2 = makeGame({ id: guid('g2'), gameNumber: 2, isFinished: true });
    const series = baseSeries([g1, g2]);

    expect(resolveClickTargetMatchId(match, series, undefined)).toBe(g2.id);
  });

  it('returns undefined for a series with no games yet (no admin UI to create one — see #35)', () => {
    const match = baseMatch({ id: guid('series') });
    const series = baseSeries([]);

    expect(resolveClickTargetMatchId(match, series, undefined)).toBeUndefined();
  });

  it('never returns the synthetic tie id — resolves to the first unfinished leg', () => {
    const tieMatch = baseMatch({ id: 'tie:stage-1:home:visitor' as GUID });
    const leg1 = baseMatch({ id: guid('leg1'), isFinished: true });
    const leg2 = baseMatch({ id: guid('leg2'), isFinished: false });

    const targetId = resolveClickTargetMatchId(tieMatch, undefined, [leg1, leg2]);

    expect(targetId).toBe(leg2.id);
    expect(targetId).not.toBe(tieMatch.id);
  });

  it('resolves a finished tie to its last leg', () => {
    const tieMatch = baseMatch({ id: 'tie:stage-1:home:visitor' as GUID });
    const leg1 = baseMatch({ id: guid('leg1'), isFinished: true });
    const leg2 = baseMatch({ id: guid('leg2'), isFinished: true });

    expect(resolveClickTargetMatchId(tieMatch, undefined, [leg1, leg2])).toBe(leg2.id);
  });
});
