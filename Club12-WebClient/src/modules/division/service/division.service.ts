import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { GenericResponsePagination } from '../../core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '../../core/utils/axiosUtils';
import {
  AddDivisionRequest,
  DivisionFiltered,
  DivisionResponse,
  DivisionTopScoreResponse,
  PutDivisionRequest,
} from '../type/division';

/**
 * DivisionService provides methods to interact with the divisions API.
 */
export const divisionService = {
  /**
   * Adds a new division.
   * @param {AddDivisionRequest} division - The division data to be added.
   * @returns {Promise<AxiosResponse<DivisionResponse>>} - A promise that resolves with the server response.
   */
  addDivision: async (
    division: AddDivisionRequest
  ): Promise<AxiosResponse<DivisionResponse>> =>
    await sendPost<DivisionResponse>(routes.divisions, division),

  /**
   * Generates the fixture for a division based on its ID.
   * @param {string} id - The ID of the division to generate the fixture for.
   * @returns {Promise<AxiosResponse<DivisionResponse>>} - A promise that resolves with the server response.
   */
  generateFixtureByDivisionId: async (
    id: string
  ): Promise<AxiosResponse<void>> =>
    await sendPost<void>(`${routes.divisions}/${id}/generate-fixture`),

  /**
   * Updates an existing division by its ID.
   * @param {string} id - The ID of the division to be updated.
   * @param {PutDivisionRequest} division - The updated division data.
   * @returns {Promise<AxiosResponse<DivisionResponse>>} - A promise that resolves with the server response.
   */
  putDivisionById: async (
    id: string,
    division: PutDivisionRequest
  ): Promise<AxiosResponse<DivisionResponse>> =>
    await sendPut<DivisionResponse>(`${routes.divisions}/${id}`, division),

  /**
   * Retrieves a division by its ID.
   * @param {string} id - The ID of the division to retrieve.
   * @returns {Promise<AxiosResponse<DivisionResponse>>} - A promise that resolves with the division data.
   */
  getDivisionsById: async (
    id: string
  ): Promise<AxiosResponse<DivisionResponse>> =>
    sendGet<DivisionResponse>(`${routes.divisions}/${id}`),

  /**
   * Retrieves divisions based on provided filters.
   * @param {DivisionFiltered} filter - The filters to apply when retrieving divisions.
   * @returns {Promise<AxiosResponse<DivisionResponse>>} - A promise that resolves with a list of divisions matching the filter.
   */
  getDivisionsByFilters: async (
    filter: DivisionFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<DivisionResponse>>> =>
    sendGet<GenericResponsePagination<DivisionResponse>>(
      routes.divisions,
      filter
    ),

  /**
   * Retrieves the top scorers for a division by its ID.
   * @param {string} id - The ID of the division to retrieve top scorers for.
   * @returns {Promise<AxiosResponse<DivisionTopScoreResponse>>} - A promise that resolves with the top scorers for the division.
   */
  getTopScoresByDivisionId: async (
    id: string
  ): Promise<AxiosResponse<DivisionTopScoreResponse>> =>
    sendGet<DivisionTopScoreResponse>(`${routes.divisions}/top-scorers/${id}`),

  /**
   * Deletes a division by its ID.
   * @param {string} id - The ID of the division to delete.
   * @returns {Promise<AxiosResponse<DivisionResponse>>} - A promise that resolves when the division is deleted.
   */
  deleteDivisionsById: async (id: string): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.divisions}/${id}`),
};
