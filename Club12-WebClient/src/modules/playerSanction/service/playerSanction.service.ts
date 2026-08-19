import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import {
  IAddPlayerSanction,
  IAppealPlayerSanction,
  IPlayerSanctionFiltered,
  IPlayerSanctionResponse,
  IPutPlayerSanction,
  IResolveAppeal,
} from '@/modules/playerSanction/type/playerSanction';

/**
 * Service for managing player sanction-related operations.
 */
export const playerSanctionService = {
  /**
   * Creates a new player sanction.
   * @param {IAddPlayerSanction} playerSanction - The sanction details to create.
   * @returns {Promise<AxiosResponse<IPlayerSanctionResponse>>} The server response.
   */
  addPlayerSanction: async (
    playerSanction: IAddPlayerSanction
  ): Promise<AxiosResponse<IPlayerSanctionResponse>> =>
    sendPost(routes.playerSanctions, playerSanction),

  /**
   * Fetches a player sanction by its unique identifier or its slug.
   * @param {string} idOrSlug - The id or slug of the player sanction to retrieve.
   * @returns {Promise<IPlayerSanctionResponse | void>} A promise that resolves with the sanction response or void if not found.
   */
  getPlayerSanctionById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IPlayerSanctionResponse>> =>
    sendGet(`${routes.playerSanctions}/${idOrSlug}`),

  /**
   * Retrieves a list of player sanctions based on a filter.
   * @param {IPlayerSanctionFiltered} filter - The filter criteria for retrieving sanctions.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IPlayerSanctionResponse>>>} The server response.
   */
  getPlayerSanctionByFilter: async (
    filter: IPlayerSanctionFiltered
  ): Promise<
    AxiosResponse<GenericResponsePagination<IPlayerSanctionResponse>>
  > => sendGet(`${routes.playerSanctions}/find`, withTablePageSize(filter)),

  /**
   * Updates an existing player sanction by its ID.
   * @param {GUID} id - The ID of the sanction to update.
   * @param {IPutPlayerSanction} playerSanction - The updated sanction details.
   * @returns {Promise<AxiosResponse<IPlayerSanctionResponse>>} The server response.
   */
  putPlayerSanctionById: async (
    id: GUID,
    playerSanction: IPutPlayerSanction
  ): Promise<AxiosResponse<IPlayerSanctionResponse>> =>
    sendPut(`${routes.playerSanctions}/${id}`, playerSanction),

  /**
   * Deletes a player sanction by its ID.
   * @param {GUID} id - The ID of the sanction to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deletePlayerSanction: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete(`${routes.playerSanctions}/${id}`),

  appealPlayerSanction: async (
    id: GUID,
    appeal: IAppealPlayerSanction
  ): Promise<AxiosResponse<IPlayerSanctionResponse>> =>
    sendPut(`${routes.playerSanctions}/${id}/appeal`, appeal),

  resolvePlayerSanctionAppeal: async (
    id: GUID,
    resolution: IResolveAppeal
  ): Promise<AxiosResponse<IPlayerSanctionResponse>> =>
    sendPut(`${routes.playerSanctions}/${id}/appeal/resolve`, resolution),
};
