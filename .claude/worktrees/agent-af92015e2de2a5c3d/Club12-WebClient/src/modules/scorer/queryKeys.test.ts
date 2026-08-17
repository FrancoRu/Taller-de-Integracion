import { describe, expect, it } from 'vitest';
import { scorerKeys } from './queryKeys';
import { IScorerByTeamFiltered, IScorerFiltered } from '@/modules/scorer/type/scorer.d';

describe('scorerKeys', () => {
  it('byTeam(filter) returns the byTeam literal', () => {
    const filter: IScorerByTeamFiltered = { pageNumber: 1 };
    expect(scorerKeys.byTeam(filter)).toEqual(['scorer', 'byTeam', filter]);
  });

  it('byPlayer(filter) returns the byPlayer literal', () => {
    const filter: IScorerFiltered = { pageNumber: 1 };
    expect(scorerKeys.byPlayer(filter)).toEqual([
      'scorer',
      'byPlayer',
      filter,
    ]);
  });
});
