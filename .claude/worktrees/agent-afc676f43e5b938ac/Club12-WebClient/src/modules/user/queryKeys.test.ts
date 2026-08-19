import { describe, expect, it } from 'vitest';
import { userKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';
import { UserFilterRequest } from '@/modules/user/type/user';

describe('userKeys', () => {
  const id: GUID = '66666666-6666-6666-6666-666666666666';

  it('list() returns the bare list literal with no trailing undefined', () => {
    expect(userKeys.list()).toEqual(['user', 'list']);
  });

  it('list(filter) returns the filtered list literal', () => {
    const filter: UserFilterRequest = { pageNumber: 1 };
    expect(userKeys.list(filter)).toEqual(['user', 'list', filter]);
  });

  it('byId(id) returns the by-id literal', () => {
    expect(userKeys.byId(id)).toEqual(['user', 'byId', id]);
  });
});
