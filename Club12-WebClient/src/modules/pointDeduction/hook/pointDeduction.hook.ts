import { useCallback, useEffect, useState } from 'react';
import { GUID } from '@/modules/core/types/types';
import { pointDeductionService } from '@/modules/pointDeduction/service/pointDeduction.service';
import {
  IAddPointDeduction,
  IPointDeductionResponse,
} from '@/modules/pointDeduction/type/pointDeduction';

/**
 * The shape returned by {@link usePointDeductions}.
 */
export interface UsePointDeductions {
  /** The division's deductions, newest first. */
  deductions: IPointDeductionResponse[];
  /** Whether a list refresh is in flight. */
  loading: boolean;
  /** Reloads the division's deductions from the server. */
  refresh: () => Promise<void>;
  /** Applies a new deduction and refreshes the list. Returns the created row. */
  create: (
    deduction: IAddPointDeduction
  ) => Promise<IPointDeductionResponse>;
  /** Removes a deduction by id and refreshes the list. */
  remove: (id: GUID) => Promise<void>;
}

/**
 * Manages the disciplinary point deductions (deducción de puntos) of a single
 * division: loads the list, and creates/removes entries. Standalone (no
 * provider needed) so it can be dropped into the division admin view. Pass a
 * falsy `divisionId` to keep it idle until the division has resolved.
 * @param divisionId - The division whose deductions to manage.
 */
export const usePointDeductions = (
  divisionId: GUID | undefined
): UsePointDeductions => {
  const [deductions, setDeductions] = useState<IPointDeductionResponse[]>([]);
  const [loading, setLoading] = useState(false);

  const refresh = useCallback(async () => {
    if (!divisionId) {
      return;
    }
    setLoading(true);
    try {
      const response =
        await pointDeductionService.getPointDeductionsByDivisionId(divisionId);
      setDeductions(response.data ?? []);
    } finally {
      setLoading(false);
    }
  }, [divisionId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const create = useCallback(
    async (
      deduction: IAddPointDeduction
    ): Promise<IPointDeductionResponse> => {
      if (!divisionId) {
        throw new Error('A division is required to add a point deduction.');
      }
      const response = await pointDeductionService.addPointDeduction(
        divisionId,
        deduction
      );
      await refresh();
      return response.data;
    },
    [divisionId, refresh]
  );

  const remove = useCallback(
    async (id: GUID): Promise<void> => {
      await pointDeductionService.deletePointDeduction(id);
      await refresh();
    },
    [refresh]
  );

  return { deductions, loading, refresh, create, remove };
};
