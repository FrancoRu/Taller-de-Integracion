import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GUID } from '@/modules/core/types/types';
import { sendDelete, sendGet, sendPost, sendPut } from '@/modules/core/utils/axiosUtils';
import {
  IClubHistoryResponse,
  IClubSummaryResponse,
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

  /**
   * Retrieves every club's stable identity summary.
   * @returns {Promise<AxiosResponse<IClubSummaryResponse[]>>} The server response.
   */
  getAllClubs: async (): Promise<AxiosResponse<IClubSummaryResponse[]>> =>
    await sendGet(routes.clubs),

  /**
   * Links a club as a squad of a parent institution club.
   * @param {GUID} childClubId - The squad club to link.
   * @param {GUID} parentClubId - The institution club it becomes a squad of.
   * @returns {Promise<AxiosResponse<IClubHistoryResponse>>} The server response.
   */
  linkClubParent: async (
    childClubId: GUID,
    parentClubId: GUID
  ): Promise<AxiosResponse<IClubHistoryResponse>> =>
    await sendPut(`${routes.clubs}/${childClubId}/parent`, { parentClubId }),

  /**
   * Clears a club's parent institution link, if any.
   * @param {GUID} childClubId - The club to unlink.
   * @returns {Promise<AxiosResponse<IClubHistoryResponse>>} The server response.
   */
  unlinkClubParent: async (
    childClubId: GUID
  ): Promise<AxiosResponse<IClubHistoryResponse>> =>
    await sendDelete(`${routes.clubs}/${childClubId}/parent`),

  /**
   * Renames a club.
   * @param {GUID} clubId - The club to rename.
   * @param {string} name - The new display name.
   * @returns {Promise<AxiosResponse<IClubHistoryResponse>>} The server response.
   */
  renameClub: async (
    clubId: GUID,
    name: string
  ): Promise<AxiosResponse<IClubHistoryResponse>> =>
    await sendPut(`${routes.clubs}/${clubId}`, { name }),
};
