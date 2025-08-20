import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '../../core/utils/axiosUtils';
import {
  IAddPlayerSanction,
  IPlayerSanctionFiltered,
  IPlayerSanctionResponse,
  IPutPlayerSanction,
} from '../type/playerSanction';

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
   * Fetches a player sanction by its unique identifier.
   * @param {GUID} id - The unique identifier of the player sanction to retrieve.
   * @returns {Promise<IPlayerSanctionResponse | void>} A promise that resolves with the sanction response or void if not found.
   */
  getPlayerSanctionById: async (
    id: GUID
  ): Promise<AxiosResponse<IPlayerSanctionResponse>> =>
    sendGet(`${routes.playerSanctions}/${id}`),

  /**
   * Retrieves a list of player sanctions based on a filter.
   * @param {IPlayerSanctionFiltered} filter - The filter criteria for retrieving sanctions.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IPlayerSanctionResponse>>>} The server response.
   */
  getPlayerSanctionByFilter: async (
    filter: IPlayerSanctionFiltered
  ): Promise<
    AxiosResponse<GenericResponsePagination<IPlayerSanctionResponse>>
  > => sendGet(`${routes.playerSanctions}/find`, filter),

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
};
