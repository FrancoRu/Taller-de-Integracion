import { describe, expect, it, vi, beforeEach } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import {
  ILoadWalkOverRequest,
  ISuspendMatchRequest,
} from '@/modules/match/type/match';

vi.mock('@/modules/core/utils/axiosUtils', () => ({
  sendGet: vi.fn(() => Promise.resolve({ data: [] })),
  sendPost: vi.fn(),
  sendPut: vi.fn(() => Promise.resolve({ data: {} })),
  sendDelete: vi.fn(),
}));

import { sendGet, sendPut } from '@/modules/core/utils/axiosUtils';
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

describe('matchService.suspendMatch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('PUTs the optional new date to matches/{id}/suspend (HU-68)', async () => {
    const matchId = guid('aaaa-bbbb-cccc-dddd-eeee');
    const request: ISuspendMatchRequest = {
      matchDate: '2026-05-01T18:00:00.000Z',
    };

    await matchService.suspendMatch(matchId, request);

    expect(sendPut).toHaveBeenCalledWith(
      `matches/${matchId}/suspend`,
      request
    );
  });

  it('suspends in place when no new date is provided', async () => {
    const matchId = guid('ffff-gggg-hhhh-iiii-jjjj');

    await matchService.suspendMatch(matchId, {});

    expect(sendPut).toHaveBeenCalledWith(`matches/${matchId}/suspend`, {});
  });
});

describe('matchService.getStageMatchesByRound', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('GETs the jornada-grouped fixture from matches/stage/{stageId}/by-round (HU-63)', async () => {
    const stageId = guid('1111-2222-3333-4444-5555');

    await matchService.getStageMatchesByRound(stageId);

    expect(sendGet).toHaveBeenCalledWith(
      `matches/stage/${stageId}/by-round`
    );
  });
});
