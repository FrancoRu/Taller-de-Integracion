import { act, renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { MatchProvider } from '@/modules/match/context/match.context';
import { useMatch } from '@/modules/match/hook/match.hook';
import { matchService } from '@/modules/match/service/match.service';
import type { GUID } from '@/modules/core/types/types';
import type { IMatchResponse } from '@/modules/match/type/match';
import { MatchType } from '@/modules/core/enum/match/matchType';

vi.mock('@/modules/match/service/match.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedPutMatchByMatchId = vi.mocked(matchService.putMatchByMatchId);
const mockedPutMatchScoreByMatchId = vi.mocked(
  matchService.putMatchScoreByMatchId
);
const mockedLoadWalkOver = vi.mocked(matchService.loadWalkOver);
const mockedSuspendMatch = vi.mocked(matchService.suspendMatch);
const mockedSwalFire = vi.mocked(Swal.fire);

const MATCH_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc' as GUID;

const MATCH: IMatchResponse = {
  id: MATCH_ID,
  matchDate: '2026-03-01T20:00:00.000Z',
  round: 1,
  matchType: MatchType.Regular,
  slug: 'match-1',
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: null,
  winningTeamName: null,
};

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>
    <ErrorProvider>
      <MatchProvider>{children}</MatchProvider>
    </ErrorProvider>
  </QueryClientProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('MatchProvider — no duplicate success toast', () => {
  /**
   * matchPage.tsx already shows its own specific "Partido actualizado" /
   * "Resultado cargado" / "Walkover cargado" toast for these three actions.
   * The context used to ALSO fire a generic (and for putMatchByMatchId,
   * mislabeled "creado") toast, so the user saw two different messages
   * back to back for one action with no way to tell what actually happened.
   */
  it('does not fire its own toast after putMatchByMatchId succeeds', async () => {
    mockedPutMatchByMatchId.mockResolvedValueOnce({
      status: 200,
      data: MATCH,
    } as never);

    const { result } = renderHook(() => useMatch(), { wrapper });
    await act(async () => {
      await result.current.putMatchByMatchId(MATCH_ID, { matchDate: MATCH.matchDate });
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after putMatchScoreByMatchId succeeds', async () => {
    mockedPutMatchScoreByMatchId.mockResolvedValueOnce({
      status: 200,
      data: MATCH,
    } as never);

    const { result } = renderHook(() => useMatch(), { wrapper });
    await act(async () => {
      await result.current.putMatchScoreByMatchId(MATCH_ID, {
        homeScore: 60,
        visitorScore: 55,
      });
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after loadWalkOver succeeds', async () => {
    mockedLoadWalkOver.mockResolvedValueOnce({
      status: 200,
      data: MATCH,
    } as never);

    const { result } = renderHook(() => useMatch(), { wrapper });
    await act(async () => {
      await result.current.loadWalkOver(MATCH_ID, {
        presentTeamId: 'dddddddd-dddd-dddd-dddd-dddddddddddd' as GUID,
      });
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('still fires its own toast after suspendMatch, its only success feedback', async () => {
    mockedSuspendMatch.mockResolvedValueOnce({
      status: 200,
      data: MATCH,
    } as never);

    const { result } = renderHook(() => useMatch(), { wrapper });
    await act(async () => {
      await result.current.suspendMatch(MATCH_ID, {});
    });

    expect(mockedSwalFire).toHaveBeenCalledTimes(1);
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Partido reprogramado correctamente' })
    );
  });
});
