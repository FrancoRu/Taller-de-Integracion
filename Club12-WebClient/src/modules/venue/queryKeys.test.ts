import { describe, expect, it } from 'vitest';
import { venueKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';

describe('venueKeys', () => {
  const id: GUID = '33333333-3333-3333-3333-333333333333';

  it('list() returns the bare list literal', () => {
    expect(venueKeys.list()).toEqual(['venue', 'list']);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(venueKeys.byId(id)).toEqual(['venue', 'byId', id]);
  });
});
