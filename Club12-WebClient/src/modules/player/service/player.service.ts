import { AxiosResponse } from "axios";
import routes from "../../core/constants/routes";
import { GenericResponsePagination } from "../../core/types/types";
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from "../../core/utils/utilsAxios";
import {
  AddPlayerRequest,
  PlayerFiltered,
  PlayerResponse,
  PutPlayerRequest,
} from "../type/player";

/**
 * Service for managing player-related operations.
 */
export const playerService = {
  /**
   * Adds a new player.
   * @param {AddPlayerRequest} player - The player details to add.
   * @returns {Promise<AxiosResponse<PlayerResponse>>} The server response.
   */
  addPlayer: async (
    player: AddPlayerRequest
  ): Promise<AxiosResponse<PlayerResponse>> =>
    await sendPost<PlayerResponse>(routes.players, player),

  /**
   * Retrieves a player by their ID.
   * @param {string} id - The ID of the player to retrieve.
   * @returns {Promise<AxiosResponse<PlayerResponse>>} The server response.
   */
  getPlayerById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendGet<void>(`${routes.players}/${id}`),

  /**
   * Retrieves a list of players based on a filter.
   * @param {PlayerFiltered} filter - The filter criteria for retrieving players.
   * @returns {Promise<AxiosResponse<PlayerResponse>>} The server response.
   */
  getPlayersByFilter: async (
    filter: PlayerFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<PlayerResponse>>> =>
    await sendGet<GenericResponsePagination<PlayerResponse>>(
      routes.players,
      filter
    ),

  /**
   * Updates the details of an existing player.
   * @param {string} id - The ID of the player to update.
   * @param {PutPlayerRequest} player - The updated player details.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putPlayerById: async (
    id: string,
    player: PutPlayerRequest
  ): Promise<AxiosResponse<void>> =>
    sendPut<void>(`${routes.players}/${id}`, player),

  /**
   * Deletes a player by their ID.
   * @param {string} id - The ID of the player to delete.
   * @returns {Promise<AxiosResponse<PlayerResponse>>} The server response.
   */
  deletePlayerById: async (id: string): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.players}/${id}`),
};
