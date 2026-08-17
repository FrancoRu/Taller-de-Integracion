import { describe, expect, it } from 'vitest';
import { playerSanctionKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { IPlayerSanctionFiltered } from '@/modules/playerSanction/type/playerSanction.d';

describe('playerSanctionKeys', () => {
  const id: GUID = '77777777-7777-7777-7777-777777777777';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(playerSanctionKeys.list()).toEqual(['playerSanction', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: IPlayerSanctionFiltered = { pageNumber: 1 };
    expect(playerSanctionKeys.list(filter)).toEqual([
      'playerSanction',
      'list',
      filter,
    ]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(playerSanctionKeys.byId(id)).toEqual(['playerSanction', 'byId', id]);
  });
});
