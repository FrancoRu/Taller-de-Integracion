import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import {
  deriveTournamentOptions,
  resolveScopeTournamentIds,
  resolveSeasonYear,
} from '@/views/panel/statisticsFilters';

const seasonA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID;
const seasonB = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' as GUID;
const t1 = '11111111-1111-1111-1111-111111111111' as GUID;
const t2 = '22222222-2222-2222-2222-222222222222' as GUID;
const t3 = '33333333-3333-3333-3333-333333333333' as GUID;

const seasons = [
  {
    id: seasonA,
    year: 2026,
    tournaments: [
      { id: t1, name: 'Apertura 2026' },
      { id: t2, name: 'Clausura 2026' },
    ],
  },
  {
    id: seasonB,
    year: null,
    tournaments: [{ id: t3, name: 'Copa sin año' }],
  },
];

const allTournaments = [
  { id: t1, name: 'Apertura 2026' },
  { id: t2, name: 'Clausura 2026' },
  { id: t3, name: 'Copa sin año' },
];

describe('deriveTournamentOptions', () => {
  it('returns every tournament when no season is selected', () => {
    expect(deriveTournamentOptions(seasons, '', allTournaments)).toEqual([
      { id: t1, name: 'Apertura 2026' },
      { id: t2, name: 'Clausura 2026' },
      { id: t3, name: 'Copa sin año' },
    ]);
  });

  it('returns only the selected season tournaments', () => {
    expect(deriveTournamentOptions(seasons, seasonA, allTournaments)).toEqual([
      { id: t1, name: 'Apertura 2026' },
      { id: t2, name: 'Clausura 2026' },
    ]);
  });

  it('returns an empty list when the selected season is unknown', () => {
    const unknown = 'cccccccc-cccc-cccc-cccc-cccccccccccc' as GUID;
    expect(deriveTournamentOptions(seasons, unknown, allTournaments)).toEqual(
      []
    );
  });

  it('handles null season/tournament sources gracefully', () => {
    expect(deriveTournamentOptions(null, '', null)).toEqual([]);
    expect(deriveTournamentOptions(null, seasonA, null)).toEqual([]);
  });
});

describe('resolveSeasonYear', () => {
  it('returns an empty string when no season is selected', () => {
    expect(resolveSeasonYear(seasons, '')).toBe('');
  });

  it('returns the calendar year of the selected season', () => {
    expect(resolveSeasonYear(seasons, seasonA)).toBe(2026);
  });

  it('returns an empty string when the season has no year', () => {
    expect(resolveSeasonYear(seasons, seasonB)).toBe('');
  });

  it('returns an empty string when the selected season is unknown', () => {
    const unknown = 'cccccccc-cccc-cccc-cccc-cccccccccccc' as GUID;
    expect(resolveSeasonYear(seasons, unknown)).toBe('');
  });
});

describe('resolveScopeTournamentIds', () => {
  it('returns null (unscoped/global) when neither filter is set', () => {
    expect(resolveScopeTournamentIds(seasons, '', '')).toBeNull();
  });

  it('scopes to just the chosen torneo, even with a season also selected', () => {
    expect(resolveScopeTournamentIds(seasons, seasonA, t1)).toEqual([t1]);
  });

  it('scopes to every tournament the chosen temporada groups', () => {
    expect(resolveScopeTournamentIds(seasons, seasonA, '')).toEqual([t1, t2]);
  });

  it('resolves to an empty (scoped-to-nothing) list for a season with no tournaments', () => {
    const empty = 'dddddddd-dddd-dddd-dddd-dddddddddddd' as GUID;
    const seasonsWithEmpty = [
      ...seasons,
      { id: empty, year: 2027, tournaments: [] },
    ];
    expect(resolveScopeTournamentIds(seasonsWithEmpty, empty, '')).toEqual([]);
  });

  it('handles a null seasons source gracefully', () => {
    expect(resolveScopeTournamentIds(null, seasonA, '')).toEqual([]);
    expect(resolveScopeTournamentIds(null, '', '')).toBeNull();
  });
});
