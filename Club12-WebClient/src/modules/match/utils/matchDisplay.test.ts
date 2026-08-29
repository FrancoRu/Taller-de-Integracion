import { describe, expect, it } from 'vitest';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';
import {
  getMatchStatusBadgeColor,
  getMatchStatusBadgeLabel,
  getScoreboardEmphasis,
  resolveMatchStatus,
  sortScorersByPoints,
} from '@/modules/match/utils/matchDisplay';

describe('matchDisplay status badge helpers', () => {
  it('maps each status to its Spanish label', () => {
    expect(getMatchStatusBadgeLabel(MatchStatus.Scheduled, false)).toBe(
      'Programado'
    );
    expect(getMatchStatusBadgeLabel(MatchStatus.Played, true)).toBe('Jugado');
    expect(getMatchStatusBadgeLabel(MatchStatus.Suspended, false)).toBe(
      'Suspendido'
    );
    expect(getMatchStatusBadgeLabel(MatchStatus.WalkOver, true)).toBe('W.O.');
  });

  it('maps each status to a distinct chip color', () => {
    expect(getMatchStatusBadgeColor(MatchStatus.Scheduled, false)).toBe(
      'default'
    );
    expect(getMatchStatusBadgeColor(MatchStatus.Played, true)).toBe('success');
    expect(getMatchStatusBadgeColor(MatchStatus.Suspended, false)).toBe(
      'warning'
    );
    expect(getMatchStatusBadgeColor(MatchStatus.WalkOver, true)).toBe('info');
  });

  it('falls back to isFinished when status is missing', () => {
    expect(resolveMatchStatus(null, true)).toBe(MatchStatus.Played);
    expect(resolveMatchStatus(undefined, false)).toBe(MatchStatus.Scheduled);
    // An explicit status always wins over the isFinished fallback.
    expect(resolveMatchStatus(MatchStatus.WalkOver, false)).toBe(
      MatchStatus.WalkOver
    );
  });
});

describe('getScoreboardEmphasis', () => {
  it('emphasises the home side when it is the winning team', () => {
    expect(
      getScoreboardEmphasis({
        isFinished: true,
        homeTeamId: 'home',
        visitorTeamId: 'visitor',
        winningTeamId: 'home',
      })
    ).toEqual({ home: 'winner', visitor: 'loser' });
  });

  it('emphasises the visitor side when it is the winning team', () => {
    expect(
      getScoreboardEmphasis({
        isFinished: true,
        homeTeamId: 'home',
        visitorTeamId: 'visitor',
        winningTeamId: 'visitor',
      })
    ).toEqual({ home: 'loser', visitor: 'winner' });
  });

  it('stays neutral while the match is not finished, even if a winner id exists', () => {
    expect(
      getScoreboardEmphasis({
        isFinished: false,
        homeTeamId: 'home',
        visitorTeamId: 'visitor',
        winningTeamId: 'home',
      })
    ).toEqual({ home: 'neutral', visitor: 'neutral' });
  });

  it('stays neutral when the match is finished but has no recorded winner', () => {
    expect(
      getScoreboardEmphasis({
        isFinished: true,
        homeTeamId: 'home',
        visitorTeamId: 'visitor',
        winningTeamId: null,
      })
    ).toEqual({ home: 'neutral', visitor: 'neutral' });
  });
});

describe('sortScorersByPoints', () => {
  it('orders scorers by points descending, then by name, without mutating the input', () => {
    const scorers = [
      { fullName: 'Ana', points: 10 },
      { fullName: 'Beto', points: 25 },
      { fullName: 'Carla', points: 10 },
    ];

    const sorted = sortScorersByPoints(scorers);

    expect(sorted.map(s => s.fullName)).toEqual(['Beto', 'Ana', 'Carla']);
    // The original array is left untouched (pure).
    expect(scorers[0].fullName).toBe('Ana');
  });
});
