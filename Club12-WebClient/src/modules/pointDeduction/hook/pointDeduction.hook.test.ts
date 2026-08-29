import { act, renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { usePointDeductions } from '@/modules/pointDeduction/hook/pointDeduction.hook';
import { pointDeductionService } from '@/modules/pointDeduction/service/pointDeduction.service';
import { IPointDeductionResponse } from '@/modules/pointDeduction/type/pointDeduction';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/pointDeduction/service/pointDeduction.service');

const mocked = vi.mocked(pointDeductionService);
const DIVISION_ID = 'division-1' as unknown as GUID;

const buildDeduction = (
  overrides: Partial<IPointDeductionResponse>
): IPointDeductionResponse => ({
  id: 'deduction-1' as unknown as GUID,
  divisionId: DIVISION_ID,
  teamId: 'team-1' as unknown as GUID,
  teamName: 'Aguará',
  points: 2,
  reason: 'Alineación indebida',
  dateCreated: '2026-01-01T00:00:00Z',
  ...overrides,
});

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const asAxios = <T,>(data: T) => ({ data }) as any;

describe('usePointDeductions', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('loads the division deductions on mount', async () => {
    mocked.getPointDeductionsByDivisionId.mockResolvedValue(
      asAxios([buildDeduction({})])
    );

    const { result } = renderHook(() => usePointDeductions(DIVISION_ID));

    await waitFor(() => expect(result.current.deductions).toHaveLength(1));
    expect(mocked.getPointDeductionsByDivisionId).toHaveBeenCalledWith(
      DIVISION_ID
    );
  });

  it('stays idle without a division id', async () => {
    const { result } = renderHook(() => usePointDeductions(undefined));

    await act(async () => {
      await result.current.refresh();
    });

    expect(mocked.getPointDeductionsByDivisionId).not.toHaveBeenCalled();
    expect(result.current.deductions).toEqual([]);
  });

  it('creates a deduction then refreshes the list', async () => {
    mocked.getPointDeductionsByDivisionId
      .mockResolvedValueOnce(asAxios([]))
      .mockResolvedValueOnce(asAxios([buildDeduction({})]));
    mocked.addPointDeduction.mockResolvedValue(asAxios(buildDeduction({})));

    const { result } = renderHook(() => usePointDeductions(DIVISION_ID));
    await waitFor(() =>
      expect(mocked.getPointDeductionsByDivisionId).toHaveBeenCalledTimes(1)
    );

    await act(async () => {
      await result.current.create({
        teamId: 'team-1' as unknown as GUID,
        points: 2,
        reason: 'Alineación indebida',
      });
    });

    expect(mocked.addPointDeduction).toHaveBeenCalledWith(DIVISION_ID, {
      teamId: 'team-1',
      points: 2,
      reason: 'Alineación indebida',
    });
    await waitFor(() => expect(result.current.deductions).toHaveLength(1));
  });

  it('removes a deduction then refreshes the list', async () => {
    mocked.getPointDeductionsByDivisionId
      .mockResolvedValueOnce(asAxios([buildDeduction({})]))
      .mockResolvedValueOnce(asAxios([]));
    mocked.deletePointDeduction.mockResolvedValue(asAxios(undefined));

    const { result } = renderHook(() => usePointDeductions(DIVISION_ID));
    await waitFor(() => expect(result.current.deductions).toHaveLength(1));

    await act(async () => {
      await result.current.remove('deduction-1' as unknown as GUID);
    });

    expect(mocked.deletePointDeduction).toHaveBeenCalledWith('deduction-1');
    await waitFor(() => expect(result.current.deductions).toHaveLength(0));
  });
});
