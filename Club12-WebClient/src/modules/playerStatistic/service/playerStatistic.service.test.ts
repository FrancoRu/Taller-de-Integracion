import { describe, expect, it, vi, beforeEach } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { LoadMatchSheetRequest } from '@/modules/playerStatistic/type/playerStatistic';

vi.mock('@/modules/core/utils/axiosUtils', () => ({
  sendGet: vi.fn(),
  sendPost: vi.fn(() => Promise.resolve({ data: [] })),
  sendPut: vi.fn(),
  sendDelete: vi.fn(),
}));

import { sendPost } from '@/modules/core/utils/axiosUtils';
import { playerStatisticService } from '@/modules/playerStatistic/service/playerStatistic.service';

const guid = (value: string) => value as GUID;

describe('playerStatisticService.loadMatchSheet', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('POSTs the whole team sheet to player-statistics/match-sheet', async () => {
    const request: LoadMatchSheetRequest = {
      matchId: guid('aaaa-bbbb-cccc-dddd-eeee'),
      teamId: guid('1111-2222-3333-4444-5555'),
      scores: [{ playerId: guid('6666-7777-8888-9999-0000'), points: 10 }],
    };

    await playerStatisticService.loadMatchSheet(request);

    expect(sendPost).toHaveBeenCalledWith(
      'player-statistics/match-sheet',
      request
    );
  });
});
