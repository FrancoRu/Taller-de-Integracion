import { describe, expect, it, vi, beforeEach } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { ILoadWalkOverRequest } from '@/modules/match/type/match';

vi.mock('@/modules/core/utils/axiosUtils', () => ({
  sendGet: vi.fn(),
  sendPost: vi.fn(),
  sendPut: vi.fn(() => Promise.resolve({ data: {} })),
  sendDelete: vi.fn(),
}));

import { sendPut } from '@/modules/core/utils/axiosUtils';
import { matchService } from '@/modules/match/service/match.service';

const guid = (value: string) => value as GUID;

describe('matchService.loadWalkOver', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('PUTs the present team to matches/{id}/walkover', async () => {
    const matchId = guid('aaaa-bbbb-cccc-dddd-eeee');
    const request: ILoadWalkOverRequest = {
      presentTeamId: guid('1111-2222-3333-4444-5555'),
    };

    await matchService.loadWalkOver(matchId, request);

    expect(sendPut).toHaveBeenCalledWith(
      `matches/${matchId}/walkover`,
      request
    );
  });
});
