import { describe, expect, it } from 'vitest';
import { authKeys } from './queryKeys';

describe('authKeys', () => {
  it('hasToken() returns the singleton literal', () => {
    expect(authKeys.hasToken()).toEqual(['auth', 'has-token']);
  });
});
