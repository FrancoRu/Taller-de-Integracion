import { describe, expect, it } from 'vitest';
import { stageKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { StageFiltered } from '@/modules/stage/type/stage';

describe('stageKeys', () => {
  const id: GUID = '55555555-5555-5555-5555-555555555555';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(stageKeys.list()).toEqual(['stage', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: StageFiltered = { pageNumber: 1 };
    expect(stageKeys.list(filter)).toEqual(['stage', 'list', filter]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(stageKeys.byId(id)).toEqual(['stage', 'byId', id]);
  });
});
