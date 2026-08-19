import { describe, expect, it } from 'vitest';
import { matchKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { MatchFiltered } from '@/modules/match/type/match';

describe('matchKeys', () => {
  const id: GUID = '44444444-4444-4444-4444-444444444444';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(matchKeys.list()).toEqual(['match', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: MatchFiltered = { pageNumber: 1 };
    expect(matchKeys.list(filter)).toEqual(['match', 'list', filter]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(matchKeys.byId(id)).toEqual(['match', 'byId', id]);
  });
});
