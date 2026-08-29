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
  IAddSeasonRequest,
  IPutSeasonRequest,
  ISeasonResponse,
  SeasonFiltered,
} from '@/modules/season/type/season';

/**
 * Service for managing seasons ("Temporadas").
 */
export const seasonService = {
  /**
   * Adds a new season.
   * @param {IAddSeasonRequest} season - The season details to add.
   * @returns {Promise<AxiosResponse<ISeasonResponse>>} The server response.
   */
  addSeason: async (
    season: IAddSeasonRequest
  ): Promise<AxiosResponse<ISeasonResponse>> =>
    await sendPost(routes.seasons, season),

  /**
   * Updates an existing season.
   * @param {string} id - The ID of the season to update.
   * @param {IPutSeasonRequest} season - The updated season details.
   * @returns {Promise<AxiosResponse<ISeasonResponse>>} The server response.
   */
  putSeasonById: async (
    id: GUID,
    season: IPutSeasonRequest
  ): Promise<AxiosResponse<ISeasonResponse>> =>
    await sendPut(`${routes.seasons}/${id}`, season),

  /**
   * Retrieves seasons based on the provided filters.
   * @param {SeasonFiltered} filter - The filters to apply when retrieving seasons.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<ISeasonResponse>>>} The server response containing the filtered seasons.
   */
  getSeasonsByFiltered: async (
    filter: SeasonFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<ISeasonResponse>>> =>
    await sendGet(routes.seasons, withTablePageSize(filter)),

  /**
   * Retrieves a season by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the season to retrieve.
   * @returns {Promise<AxiosResponse<ISeasonResponse>>} The server response containing the season details.
   */
  getSeasonById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<ISeasonResponse>> =>
    await sendGet(`${routes.seasons}/${idOrSlug}`),

  /**
   * Deletes a season by its ID.
   * @param {string} id - The ID of the season to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteSeasonById: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.seasons}/${id}`),
};
