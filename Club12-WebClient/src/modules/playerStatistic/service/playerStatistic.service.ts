import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import {
  AddPlayerStatisticRequest,
  PlayerStatisticFiltered,
  PlayerStatisticResponse,
  PutPlayerStatisticRequest,
} from '@/modules/playerStatistic/type/playerStatistic';

/**
 * Service for managing player statistics.
 */
export const playerStatisticService = {
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
    statisticid: GUID,
    playerStatistic: PutPlayerStatisticRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPut(`${routes.playerStatistics}/${statisticid}`, playerStatistic),

  /**
   * Retrieves a player statistic by its ID.
   * @param {string} id - The ID of the player statistic to retrieve.
   * @returns {Promise<AxiosResponse<PlayerStatisticResponse>>} The server response.
   */
  getPlayerStatisticById: async (
    id: GUID
  ): Promise<AxiosResponse<PlayerStatisticResponse>> =>
    await sendGet(`${routes.playerStatistics}/${id}`),

  /**
   * Retrieves player statistics based on filters.
   * @param {PlayerStatisticFiltered} filter - The filter criteria to apply.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<PlayerStatisticResponse>>>} The server response.
   */
  getPlayerStatisticsByFilter: async (
    filter: PlayerStatisticFiltered
  ): Promise<
    AxiosResponse<GenericResponsePagination<PlayerStatisticResponse>>
  > => await sendGet(routes.playerStatistics, withTablePageSize(filter)),

  /**
   * Deletes a player statistic by its ID.
   * @param {string} id - The ID of the player statistic to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deletePlayerStatisticById: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.playerStatistics}/${id}`),
};
