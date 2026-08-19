import { describe, expect, it } from 'vitest';
import { divisionKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { DivisionFiltered } from '@/modules/division/type/division';

describe('divisionKeys', () => {
  const id: GUID = '88888888-8888-8888-8888-888888888888';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(divisionKeys.list()).toEqual(['division', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: DivisionFiltered = { pageNumber: 1 };
    expect(divisionKeys.list(filter)).toEqual(['division', 'list', filter]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(divisionKeys.byId(id)).toEqual(['division', 'byId', id]);
  });

  it('topScorers(id) returns the top-scorers literal', () => {
    expect(divisionKeys.topScorers(id)).toEqual([
      'division',
      'top-scorers',
      id,
    ]);
  });
});
