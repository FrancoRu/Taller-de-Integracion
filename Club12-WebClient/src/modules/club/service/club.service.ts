import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GUID } from '@/modules/core/types/types';
import { sendGet, sendPost } from '@/modules/core/utils/axiosUtils';
import {
  IClubHistoryResponse,
  IRosterCopyRequest,
  IRosterCopyResult,
} from '@/modules/club/type/club.d';

/**
 * Service for the stable cross-season club identity (HU-99) and roster
 * cloning between seasons (HU-53).
 */
export const clubService = {
  /**
   * Retrieves a club and its trajectory across seasons.
   * @param {string} idOrSlug - The club's GUID id or its slug.
   * @returns {Promise<AxiosResponse<IClubHistoryResponse>>} The server response.
   */
  getClubHistory: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IClubHistoryResponse>> =>
    await sendGet(`${routes.clubs}/${idOrSlug}`),

  /**
   * Clones a roster from a previous season's team into the target team.
   * @param {GUID} targetTeamId - The team to copy the roster into.
   * @param {IRosterCopyRequest} request - The source team + season and target season.
   * @returns {Promise<AxiosResponse<IRosterCopyResult>>} The server response.
   */
  copyRoster: async (
    targetTeamId: GUID,
    request: IRosterCopyRequest
  ): Promise<AxiosResponse<IRosterCopyResult>> =>
    await sendPost(`${routes.teams}/${targetTeamId}/roster/copy`, request),
};
