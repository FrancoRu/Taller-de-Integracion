import { describe, expect, it } from 'vitest';
import { playerStatisticKeys } from './queryKeys';

describe('playerStatisticKeys', () => {
  it('all returns the bare invalidate-all literal', () => {
    expect(playerStatisticKeys.all).toEqual(['playerStatistic']);
  });
});
