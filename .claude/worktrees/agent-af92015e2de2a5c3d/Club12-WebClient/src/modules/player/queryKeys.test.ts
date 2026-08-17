import { describe, expect, it } from 'vitest';
import { playerKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { PlayerFiltered } from '@/modules/player/type/player.d';

describe('playerKeys', () => {
  const id: GUID = '99999999-9999-9999-9999-999999999999';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(playerKeys.list()).toEqual(['player', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: PlayerFiltered = { pageNumber: 1 };
    expect(playerKeys.list(filter)).toEqual(['player', 'list', filter]);
  });

  it('byId(id) returns the 3-element by-id literal when isAdministrative is omitted', () => {
    expect(playerKeys.byId(id)).toEqual(['player', 'byId', id]);
  });

  it('byId(id, isAdministrative) returns the 4-element by-id literal', () => {
    expect(playerKeys.byId(id, true)).toEqual(['player', 'byId', id, true]);
    expect(playerKeys.byId(id, false)).toEqual(['player', 'byId', id, false]);
  });
});
