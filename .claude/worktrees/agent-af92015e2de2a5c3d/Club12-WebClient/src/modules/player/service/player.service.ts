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
  IAddPlayerRequest,
  PlayerFiltered,
  IPlayerResponse,
  IPutPlayerRequest,
} from '@/modules/player/type/player';

/**
 * Service for managing player-related operations.
 */
export const playerService = {
  /**
   * Adds a new player.
   * @param {IAddPlayerRequest} player - The player details to add.
   * @returns {Promise<AxiosResponse<IPlayerResponse>>} The server response.
   */
  addPlayer: async (
    player: IAddPlayerRequest
  ): Promise<AxiosResponse<IPlayerResponse>> =>
    sendPost(routes.players, player),

  /**
   * Retrieves a player by their ID.
   * @param {string} id - The ID of the player to retrieve.
   * @param {boolean} isAdministrative - Whether to fetch the player using the administrative route.
   * @returns {Promise<AxiosResponse<IPlayerResponse>>} The server response.
   */
  getPlayerById: async (
    id: GUID,
    isAdministrative: boolean
  ): Promise<AxiosResponse<IPlayerResponse>> => {
    const resource: string = isAdministrative
      ? `${routes.players}/admin`
      : routes.players;
    return sendGet(`${resource}/${id}`);
  },

  /**
   * Retrieves a list of players based on a filter.
   * @param {PlayerFiltered} filter - The filter criteria for retrieving players.
   * @returns {Promise<AxiosResponse<IPlayerResponse>>} The server response.
   */
  getPlayersByFilter: async (
    filter: PlayerFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IPlayerResponse>>> =>
    sendGet(routes.players, withTablePageSize(filter)),

  /**
   * Updates the details of an existing player.
   * @param {string} id - The ID of the player to update.
   * @param {IPutPlayerRequest} player - The updated player details.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putPlayerById: async (
    id: GUID,
    player: IPutPlayerRequest
  ): Promise<AxiosResponse<IPlayerResponse>> =>
    sendPut(`${routes.players}/${id}`, player),

  /**
   * Deletes a player by their ID.
   * @param {string} id - The ID of the player to delete.
   * @returns {Promise<AxiosResponse<IPlayerResponse>>} The server response.
   */
  deletePlayerById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete(`${routes.players}/${id}`),
};
