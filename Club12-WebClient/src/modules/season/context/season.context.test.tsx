import { act, renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { SeasonProvider } from '@/modules/season/context/season.context';
import { useSeason } from '@/modules/season/hook/season.hook';
import { seasonService } from '@/modules/season/service/season.service';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/season/service/season.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedPutSeasonById = vi.mocked(seasonService.putSeasonById);
const mockedDeleteSeasonById = vi.mocked(seasonService.deleteSeasonById);
const mockedSwalFire = vi.mocked(Swal.fire);

const SEASON_ID = '66666666-6666-6666-6666-666666666666' as GUID;

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>
    <ErrorProvider>
      <SeasonProvider>{children}</SeasonProvider>
    </ErrorProvider>
  </QueryClientProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('SeasonProvider — no duplicate success toast', () => {
  /**
   * AdminSeasonDetailPage / SeasonsPage already show their own confirmation
   * for these actions. The context used to ALSO fire a toast, so the user saw
   * two modals with the same message for one action.
   */
  it('does not fire its own toast after putSeasonById succeeds (204)', async () => {
    mockedPutSeasonById.mockResolvedValueOnce({ status: 204 } as never);

    const { result } = renderHook(() => useSeason(), { wrapper });
    await act(async () => {
      await result.current.putSeasonById(SEASON_ID, {
        name: 'Temporada XXVI',
      } as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after putSeasonById succeeds (200)', async () => {
    mockedPutSeasonById.mockResolvedValueOnce({
      status: 200,
      data: { id: SEASON_ID, slug: 's', name: 'Temporada XXVI', year: 2026, tournaments: [] },
    } as never);

    const { result } = renderHook(() => useSeason(), { wrapper });
    await act(async () => {
      await result.current.putSeasonById(SEASON_ID, {
        name: 'Temporada XXVI',
      } as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after deleteSeasonById succeeds', async () => {
    mockedDeleteSeasonById.mockResolvedValueOnce({ status: 204 } as never);

    const { result } = renderHook(() => useSeason(), { wrapper });
    await act(async () => {
      await result.current.deleteSeasonById(SEASON_ID);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });
});
