import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GUID } from '@/modules/core/types/types';
import { sendDelete, sendGet, sendPost } from '@/modules/core/utils/axiosUtils';
import {
  IAddPointDeduction,
  IPointDeductionResponse,
} from '@/modules/pointDeduction/type/pointDeduction';

/**
 * Service for the disciplinary point-deduction (deducción de puntos)
 * endpoints. Creating and deleting require AdminOrOwner; listing is public.
 */
export const pointDeductionService = {
  /**
   * Applies a point deduction to a team within a division.
   * @param {GUID} divisionId - The division whose standings the penalty affects.
   * @param {IAddPointDeduction} deduction - The team, points and reason.
   * @returns {Promise<AxiosResponse<IPointDeductionResponse>>} The created deduction.
   */
  addPointDeduction: async (
    divisionId: GUID,
    deduction: IAddPointDeduction
  ): Promise<AxiosResponse<IPointDeductionResponse>> =>
    sendPost<IPointDeductionResponse>(
      `${routes.divisions}/${divisionId}/point-deductions`,
      deduction
    ),

  /**
   * Lists every point deduction applied in a division (newest first).
   * @param {GUID} divisionId - The division whose deductions to list.
   * @returns {Promise<AxiosResponse<IPointDeductionResponse[]>>} The deductions.
   */
  getPointDeductionsByDivisionId: async (
    divisionId: GUID
  ): Promise<AxiosResponse<IPointDeductionResponse[]>> =>
    sendGet<IPointDeductionResponse[]>(
      `${routes.divisions}/${divisionId}/point-deductions`
    ),

  /**
   * Removes a point deduction by its id.
   * @param {GUID} id - The id of the deduction to remove.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deletePointDeduction: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.pointDeductions}/${id}`),
};
