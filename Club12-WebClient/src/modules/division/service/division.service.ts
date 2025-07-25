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
  AddDivisionRequest,
  DivisionFiltered,
  IDivisionResponse,
  DivisionTopScoreResponse,
  IPutDivisionRequest,
} from '../type/division';

/**
 * DivisionService provides methods to interact with the divisions API.
 */
export const divisionService = {
  /**
   * Adds a new division.
   * @param {AddDivisionRequest} division - The division data to be added.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the server response.
   */
  addDivision: async (
    division: AddDivisionRequest
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    sendPost<IDivisionResponse>(routes.divisions, division),

  /**
   * Generates the fixture for a division based on its ID.
   * @param {string} id - The ID of the division to generate the fixture for.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the server response.
   */
  generateFixtureByDivisionId: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendPost<void>(`${routes.divisions}/${id}/generate-fixture`),

  /**
   * Updates an existing division by its ID.
   * @param {string} id - The ID of the division to be updated.
   * @param {IPutDivisionRequest} division - The updated division data.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the server response.
   */
  putDivisionById: async (
    id: GUID,
    division: IPutDivisionRequest
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    sendPut<IDivisionResponse>(`${routes.divisions}/${id}`, division),

  /**
   * Retrieves a division by its ID.
   * @param {string} id - The ID of the division to retrieve.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the division data.
   */
  getDivisionsById: async (
    id: GUID
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    sendGet<IDivisionResponse>(`${routes.divisions}/${id}/detail`),

  /**
   * Retrieves divisions based on provided filters.
   * @param {DivisionFiltered} filter - The filters to apply when retrieving divisions.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with a list of divisions matching the filter.
   */
  getDivisionsByFilters: async (
    filter: DivisionFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IDivisionResponse>>> =>
    sendGet<GenericResponsePagination<IDivisionResponse>>(
      routes.divisions,
      filter
    ),

  /**
   * Retrieves the top scorers for a division by its ID.
   * @param {string} id - The ID of the division to retrieve top scorers for.
   * @returns {Promise<AxiosResponse<DivisionTopScoreResponse>>} - A promise that resolves with the top scorers for the division.
   */
  getTopScoresByDivisionId: async (
    id: GUID
  ): Promise<AxiosResponse<DivisionTopScoreResponse>> =>
    sendGet<DivisionTopScoreResponse>(`${routes.divisions}/top-scorers/${id}`),

  /**
   * Deletes a division by its ID.
   * @param {string} id - The ID of the division to delete.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves when the division is deleted.
   */
  deleteDivisionsById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.divisions}/${id}`),
};
