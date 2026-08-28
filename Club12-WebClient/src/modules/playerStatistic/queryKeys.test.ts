import { describe, expect, it } from 'vitest';
import { playerStatisticKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';

const playerId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' as GUID;

describe('playerStatisticKeys', () => {
  it('all returns the bare invalidate-all literal', () => {
    expect(playerStatisticKeys.all).toEqual(['playerStatistic']);
  });

  it('card(playerId) returns the per-player card literal', () => {
    expect(playerStatisticKeys.card(playerId)).toEqual([
      'playerStatistic',
      'card',
      playerId,
    ]);
  });

  it('history(playerId) returns the per-player history literal', () => {
    expect(playerStatisticKeys.history(playerId)).toEqual([
      'playerStatistic',
      'history',
      playerId,
    ]);
  });
});
