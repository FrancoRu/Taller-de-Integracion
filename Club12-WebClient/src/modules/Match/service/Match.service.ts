import routes from "../../core/constants/envVariables";
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from "../../core/utils/utilsAxios";
import {
  AddMatchRequest,
  MatchFiltered,
  PutMatchDateRequest,
  PutMatchScoreRequest,
} from "../type/Match";

/**
 * MatchService provides methods to interact with the matches API.
 */
export const MatchService = {
  /**
   * Adds a new match.
   * @param {AddMatchRequest} match - The match data to be added.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  addMatch: async (match: AddMatchRequest): Promise<any> =>
    await sendPost(routes.matches, match),

  /**
   * Updates the score of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {PutMatchScoreRequest} matchScore - The new match score data.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  putMatchScoreRequest: async (
    id: string,
    matchScore: PutMatchScoreRequest
  ): Promise<any> => await sendPut(`${routes.matches}/${id}/score`, matchScore),

  /**
   * Updates the date of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {PutMatchDateRequest} matchDate - The new match date data.
   * @returns {Promise} - A promise that resolves with the server response.
   */
  putMatchDate: async (
    id: string,
    matchDate: PutMatchDateRequest
  ): Promise<any> => await sendPut(`${routes.matches}/${id}/date`, matchDate),

  /**
   * Retrieves a match by its ID.
   * @param {string} id - The ID of the match to retrieve.
   * @returns {Promise} - A promise that resolves with the match data.
   */
  getMatchById: async (id: string): Promise<any> =>
    await sendGet(`${routes.matches}/${id}`),

  /**
   * Retrieves matches based on the provided filter.
   * @param {MatchFiltered} filter - The filter to apply when retrieving matches.
   * @returns {Promise} - A promise that resolves with a list of matches matching the filter.
   */
  getMatchByFilter: async (filter: MatchFiltered): Promise<any> =>
    await sendGet(routes.matches, filter),

  /**
   * Deletes a match by its ID.
   * @param {string} id - The ID of the match to delete.
   * @returns {Promise} - A promise that resolves when the match is deleted.
   */
  deleteMatch: async (id: string): Promise<any> =>
    await sendDelete(`${routes.matches}/${id}`),
};
