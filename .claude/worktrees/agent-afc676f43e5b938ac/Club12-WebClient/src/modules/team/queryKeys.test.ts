import { describe, expect, it } from 'vitest';
import { teamKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { TeamFiltered } from '@/modules/team/type/team.d';

describe('teamKeys', () => {
  const id: GUID = '22222222-2222-2222-2222-222222222222';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(teamKeys.list()).toEqual(['team', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: TeamFiltered = { name: 'Boca', pageNumber: 1 };
    expect(teamKeys.list(filter)).toEqual(['team', 'list', filter]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(teamKeys.byId(id)).toEqual(['team', 'byId', id]);
  });
});
