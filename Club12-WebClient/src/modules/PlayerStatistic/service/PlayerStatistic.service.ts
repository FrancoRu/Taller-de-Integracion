import { AxiosResponse } from "axios";
import routes from "../../core/constants/routes";
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from "../../core/utils/utilsAxios";
import {
  AddPlayerStatisticRequest,
  PlayerStatisticResponse,
  PutPlayerStatisticRequest,
} from "../type/playerStatistic";

/**
 * Service for managing player statistics.
 */
export const PlayerStatisticService = {
  /**
   * Adds a new player statistic.
   * @param {AddPlayerStatisticRequest} playerStatistic - The player statistic details to add.
   * @returns {Promise<AxiosResponse<PlayerStatisticResponse>>} The server response.
   */
  addPlayerStatistic: async (
    playerStatistic: AddPlayerStatisticRequest
  ): Promise<AxiosResponse<PlayerStatisticResponse>> =>
    await sendPost(routes.playerStatistics, playerStatistic),

  /**
   * Updates an existing player statistic.
   * @param {string} statisticId - The unique identifier of the player statistic to update.
   * @param {PutPlayerStatisticRequest} playerStatistic - The updated player statistic details.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putPlayerStatisticById: async (
    statisticId: string,
    playerStatistic: PutPlayerStatisticRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPut(`${routes.playerStatistics}/${statisticId}`, playerStatistic),

  /**
   * Retrieves a player statistic by its ID.
   * @param {string} id - The ID of the player statistic to retrieve.
   * @returns {Promise<AxiosResponse<PlayerStatisticResponse>>} The server response.
   */
  getPlayerStatisticById: async (
    id: string
  ): Promise<AxiosResponse<PlayerStatisticResponse>> =>
    await sendGet(`${routes.playerStatistics}/${id}`),

  /**
   * Deletes a player statistic by its ID.
   * @param {string} id - The ID of the player statistic to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deletePlayerStatisticById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.playerStatistics}/${id}`),
};
