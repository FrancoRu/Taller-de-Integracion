import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import {
  byeTeamNamesForRound,
  collectStageTeamNames,
  formatRoundLabel,
  groupMatchesByRound,
} from '@/modules/match/utils/matchGrouping';

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

const match = (overrides: Partial<IMatchResponse>): IMatchResponse => ({
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
  stageId: guid('stage-1'),
  winningTeamName: null,
  status: null,
  ...overrides,
});

describe('groupMatchesByRound', () => {
  it('groups matches by jornada (round), ordered ascending — not by calendar date', () => {
    const matches = [
      match({ round: 2, matchDate: '2026-04-20T18:00:00Z', homeTeam: team('C'), visitorTeam: team('D') }),
      match({ round: 1, matchDate: '2026-05-10T18:00:00Z', homeTeam: team('A'), visitorTeam: team('B') }),
      match({ round: 1, matchDate: '2026-05-10T20:00:00Z', homeTeam: team('C'), visitorTeam: team('D') }),
    ];

    const rounds = groupMatchesByRound(matches);

    expect(rounds.map(round => round.round)).toEqual([1, 2]);
    expect(rounds[0].matches).toHaveLength(2);
    expect(rounds[1].matches).toHaveLength(1);
  });

  it('places matches with a null round (knockout) in a trailing group', () => {
    const matches = [
      match({ round: null }),
      match({ round: 1 }),
    ];

    const rounds = groupMatchesByRound(matches);

    expect(rounds.map(round => round.round)).toEqual([1, null]);
  });
});

describe('formatRoundLabel', () => {
  it('renders "Fecha N" for a numbered jornada', () => {
    expect(formatRoundLabel(1)).toBe('Fecha 1');
    expect(formatRoundLabel(18)).toBe('Fecha 18');
  });

  it('renders a generic label for the null (knockout) group', () => {
    expect(formatRoundLabel(null)).toBe('Fase final');
  });
});

describe('byeTeamNamesForRound', () => {
  it('reports the team with no match that round as "Libre" (odd roster)', () => {
    const matches = [
      match({ round: 1, homeTeam: team('A'), visitorTeam: team('B') }),
      match({ round: 2, homeTeam: team('C'), visitorTeam: team('A') }),
      match({ round: 3, homeTeam: team('B'), visitorTeam: team('C') }),
    ];
    const stageTeamNames = collectStageTeamNames(matches);

    // Round 1 pairs A vs B, so C sits out.
    const round1 = matches.filter(item => item.round === 1);
    expect(byeTeamNamesForRound(round1, stageTeamNames)).toEqual(['C']);
  });

  it('returns no byes when every team plays that round (even roster)', () => {
    const matches = [
      match({ round: 1, homeTeam: team('A'), visitorTeam: team('B') }),
      match({ round: 1, homeTeam: team('C'), visitorTeam: team('D') }),
    ];
    const stageTeamNames = collectStageTeamNames(matches);

    expect(byeTeamNamesForRound(matches, stageTeamNames)).toEqual([]);
  });
});
