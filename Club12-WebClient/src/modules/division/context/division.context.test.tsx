import { act, renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { DivisionProvider } from '@/modules/division/context/division.context';
import { useDivision } from '@/modules/division/hook/division.hook';
import { divisionService } from '@/modules/division/service/division.service';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/division/service/division.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedPutDivisionById = vi.mocked(divisionService.putDivisionById);
const mockedDeleteDivisionsById = vi.mocked(divisionService.deleteDivisionsById);
const mockedSwalFire = vi.mocked(Swal.fire);

const DIVISION_ID = '55555555-5555-5555-5555-555555555555' as GUID;

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>
    <ErrorProvider>
      <DivisionProvider>{children}</DivisionProvider>
    </ErrorProvider>
  </QueryClientProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('DivisionProvider — no duplicate success toast', () => {
  /**
   * divisionEditPage.tsx / divisionsPage.tsx already show their own
   * confirmation for these actions. The context used to ALSO fire a toast, so
   * the user saw two modals with the same message for one action.
   */
  it('does not fire its own toast after putDivisionById succeeds (200)', async () => {
    mockedPutDivisionById.mockResolvedValueOnce({
      status: 200,
      data: { id: DIVISION_ID, name: 'Zona B' },
    } as never);

    const { result } = renderHook(() => useDivision(), { wrapper });
    await act(async () => {
      await result.current.putDivisionById(DIVISION_ID, {
        name: 'Zona B',
      } as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after putDivisionById succeeds (204)', async () => {
    mockedPutDivisionById.mockResolvedValueOnce({ status: 204 } as never);

    const { result } = renderHook(() => useDivision(), { wrapper });
    await act(async () => {
      await result.current.putDivisionById(DIVISION_ID, {
        name: 'Zona B',
      } as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after deleteDivisionsById succeeds', async () => {
    mockedDeleteDivisionsById.mockResolvedValueOnce({ status: 204 } as never);

    const { result } = renderHook(() => useDivision(), { wrapper });
    await act(async () => {
      await result.current.deleteDivisionsById(DIVISION_ID);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });
});
