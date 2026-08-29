import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { TeamMatch } from '@/modules/team/type/teamProfile.d';
import {
  computeRecord,
  deriveStreak,
  formatDifferential,
  formatPosition,
  formatRecord,
  splitFixture,
} from '@/modules/team/utils/teamProfile';

const guid = (value: string) => value as GUID;

const match = (overrides: Partial<TeamMatch> = {}): TeamMatch => ({
  matchId: guid('11111111-1111-1111-1111-111111111111'),
  matchDate: '2025-01-01T20:00:00Z',
  isFinished: true,
  status: 'Finished',
  isHome: true,
  opponentTeamId: guid('22222222-2222-2222-2222-222222222222'),
  opponentName: 'Rival',
  opponentLogoUrl: null,
  teamScore: 80,
  opponentScore: 70,
  result: 'W',
  venueName: 'Gimnasio',
  ...overrides,
});

describe('deriveStreak', () => {
  it('returns only finished results, newest last, capped at 5', () => {
    // 7 finished matches in ascending date order, alternating W/L, plus a
    // future (unfinished) one that must be ignored.
    const matches: TeamMatch[] = [
      match({ matchDate: '2025-01-01T00:00:00Z', result: 'W' }),
      match({ matchDate: '2025-01-02T00:00:00Z', result: 'L' }),
      match({ matchDate: '2025-01-03T00:00:00Z', result: 'W' }),
      match({ matchDate: '2025-01-04T00:00:00Z', result: 'W' }),
      match({ matchDate: '2025-01-05T00:00:00Z', result: 'L' }),
      match({ matchDate: '2025-01-06T00:00:00Z', result: 'W' }),
      match({ matchDate: '2025-01-07T00:00:00Z', result: 'L' }),
      match({
        matchDate: '2025-02-01T00:00:00Z',
        isFinished: false,
        result: null,
        teamScore: null,
        opponentScore: null,
      }),
    ];

    // Last 5 finished, chronological (oldest -> newest, left to right).
    expect(deriveStreak(matches)).toEqual(['W', 'W', 'L', 'W', 'L']);
  });

  it('ignores finished matches that somehow lack a result', () => {
    const matches: TeamMatch[] = [
      match({ result: 'W' }),
      match({ isFinished: true, result: null }),
      match({ result: 'L' }),
    ];

    expect(deriveStreak(matches)).toEqual(['W', 'L']);
  });

  it('returns an empty array when there are no finished matches', () => {
    expect(deriveStreak([match({ isFinished: false, result: null })])).toEqual(
      []
    );
    expect(deriveStreak([])).toEqual([]);
  });
});

describe('splitFixture', () => {
  it('splits into upcoming (not finished, ascending) and recent (finished, newest first, capped at 5)', () => {
    const upcomingA = match({
      matchId: guid('aaaaaaaa-0000-0000-0000-000000000001'),
      matchDate: '2025-03-01T00:00:00Z',
      isFinished: false,
      result: null,
      teamScore: null,
      opponentScore: null,
    });
    const upcomingB = match({
      matchId: guid('aaaaaaaa-0000-0000-0000-000000000002'),
      matchDate: '2025-03-08T00:00:00Z',
      isFinished: false,
      result: null,
      teamScore: null,
      opponentScore: null,
    });
    const finished = Array.from({ length: 6 }, (_, i) =>
      match({
        matchId: guid(`bbbbbbbb-0000-0000-0000-00000000000${i}`),
        matchDate: `2025-01-0${i + 1}T00:00:00Z`,
        result: i % 2 === 0 ? 'W' : 'L',
      })
    );

    // Input arrives date-ascending: finished first, then the two upcoming.
    const { upcoming, recent } = splitFixture([...finished, upcomingA, upcomingB]);

    expect(upcoming.map(m => m.matchId)).toEqual([
      upcomingA.matchId,
      upcomingB.matchId,
    ]);

    // Recent is capped to the 5 most recent finished, newest first.
    expect(recent).toHaveLength(5);
    expect(recent[0].matchId).toBe(finished[5].matchId);
    expect(recent[4].matchId).toBe(finished[1].matchId);
  });

  it('handles a team with only upcoming matches', () => {
    const { upcoming, recent } = splitFixture([
      match({ isFinished: false, result: null }),
    ]);

    expect(upcoming).toHaveLength(1);
    expect(recent).toHaveLength(0);
  });
});

describe('computeRecord', () => {
  it('aggregates wins, losses, played and points across all finished matches', () => {
    const matches: TeamMatch[] = [
      match({ result: 'W', teamScore: 80, opponentScore: 70 }),
      match({ result: 'W', teamScore: 90, opponentScore: 60 }),
      match({ result: 'L', teamScore: 55, opponentScore: 65 }),
      // Unfinished: ignored entirely.
      match({
        isFinished: false,
        result: null,
        teamScore: null,
        opponentScore: null,
      }),
    ];

    expect(computeRecord(matches)).toEqual({
      wins: 2,
      losses: 1,
      played: 3,
      pointsFor: 225,
      pointsAgainst: 195,
      pointsDifference: 30,
    });
  });

  it('counts a finished match with no score as played but adds no points', () => {
    const matches: TeamMatch[] = [
      match({ result: 'W', teamScore: null, opponentScore: null }),
    ];

    expect(computeRecord(matches)).toEqual({
      wins: 1,
      losses: 0,
      played: 1,
      pointsFor: 0,
      pointsAgainst: 0,
      pointsDifference: 0,
    });
  });

  it('is all zeros when there are no finished matches', () => {
    expect(computeRecord([])).toEqual({
      wins: 0,
      losses: 0,
      played: 0,
      pointsFor: 0,
      pointsAgainst: 0,
      pointsDifference: 0,
    });
  });
});

describe('formatRecord', () => {
  it('joins wins and losses with a dash', () => {
    expect(formatRecord(5, 2)).toBe('5-2');
    expect(formatRecord(0, 0)).toBe('0-0');
  });
});

describe('formatPosition', () => {
  it('appends the Spanish ordinal marker', () => {
    expect(formatPosition(1)).toBe('1º');
    expect(formatPosition(3)).toBe('3º');
  });
});

describe('formatDifferential', () => {
  it('prefixes a plus sign for non-negative values and keeps the minus for negatives', () => {
    expect(formatDifferential(12)).toBe('+12');
    expect(formatDifferential(0)).toBe('0');
    expect(formatDifferential(-5)).toBe('-5');
  });
});
