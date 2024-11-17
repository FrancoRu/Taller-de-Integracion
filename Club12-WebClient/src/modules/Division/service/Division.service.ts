import routes from "../../core/constants/envVariables";
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from "../../core/utils/utilsAxios";
import {
  AddDivisionRequest,
  DivisionFiltered,
  PutDivisionRequest,
} from "../type/Division";

/**
 * DivisionService provides methods to interact with the divisions API.
 */
export const DivisionService = {
  /**
   * Adds a new division.
   * @param {AddDivisionRequest} division - The division data to be added.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  addDivision: async (division: AddDivisionRequest): Promise<any> =>
    await sendPost(routes.divisions, division),

  /**
   * Generates the fixture for a division based on its ID.
   * @param {string} id - The ID of the division to generate the fixture for.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  generateFixture: async (id: string): Promise<any> =>
    await sendPost(`${routes.divisions}/${id}/generate-fixture`),

  /**
   * Updates an existing division by its ID.
   * @param {string} id - The ID of the division to be updated.
   * @param {PutDivisionRequest} division - The updated division data.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  putDivisionById: async (
    id: string,
    division: PutDivisionRequest
  ): Promise<any> => await sendPut(`${routes.divisions}/${id}`, division),

  /**
   * Retrieves a division by its ID.
   * @param {string} id - The ID of the division to retrieve.
   * @returns {Promise} - A promise that resolves with the division data.
   */
  getDivisionsById: async (id: string): Promise<any> =>
    sendGet(`${routes.divisions}/${id}`),

  /**
   * Retrieves divisions based on provided filters.
   * @param {DivisionFiltered} filter - The filters to apply when retrieving divisions.
   * @returns {Promise} - A promise that resolves with a list of divisions matching the filter.
   */
  getDivisionsByFilters: async (filter: DivisionFiltered): Promise<any> =>
    sendGet(routes.divisions, filter),

  /**
   * Retrieves the top scorers for a division by its ID.
   * @param {string} id - The ID of the division to retrieve top scorers for.
   * @returns {Promise} - A promise that resolves with the top scorers for the division.
   */
  getTopScores: async (id: string): Promise<any> =>
    sendGet(`${routes.divisions}/top-scorers/${id}`),

  /**
   * Deletes a division by its ID.
   * @param {string} id - The ID of the division to delete.
   * @returns {Promise} - A promise that resolves when the division is deleted.
   */
  deleteDivisionsById: async (id: string): Promise<any> =>
    sendDelete(`${routes.divisions}/${id}`),
};
