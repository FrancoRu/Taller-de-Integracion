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
  AddMatchRequest,
  MatchFiltered,
  MatchResponse,
  PutMatchDateRequest,
  PutMatchScoreRequest,
} from "../type/match";

/**
 * MatchService provides methods to interact with the matches API.
 */
export const matchService = {
  /**
   * Adds a new match.
   * @param {AddMatchRequest} match - The match data to be added.
   * @returns {Promise<AxiosResponse<MatchResponse>>} - A promise that resolves with the server response.
   */
  addMatch: async (
    match: AddMatchRequest
  ): Promise<AxiosResponse<MatchResponse>> =>
    await sendPost<MatchResponse>(routes.matches, match),

  /**
   * Updates the score of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {PutMatchScoreRequest} matchScore - The new match score data.
   * @returns {Promise<AxiosResponse<void>>} - A promise that resolves with the server response.
   */
  putMatchScoreByMatchId: async (
    id: string,
    matchScore: PutMatchScoreRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPut<void>(`${routes.matches}/${id}/score`, matchScore),

  /**
   * Updates the date of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {PutMatchDateRequest} matchDate - The new match date data.
   * @returns {Promise<AxiosResponse<MatchResponse>>} - A promise that resolves with the server response.
   */
  putMatchDateByMatchId: async (
    id: string,
    matchDate: PutMatchDateRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPut<void>(`${routes.matches}/${id}/date`, matchDate),

  /**
   * Retrieves a match by its ID.
   * @param {string} id - The ID of the match to retrieve.
   * @returns {Promise<AxiosResponse<MatchResponse>>} - A promise that resolves with the match data.
   */
  getMatchById: async (id: string): Promise<AxiosResponse<MatchResponse>> =>
    await sendGet<MatchResponse>(`${routes.matches}/${id}`),

  /**
   * Retrieves matches based on the provided filter.
   * @param {MatchFiltered} filter - The filter to apply when retrieving matches.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<MatchResponse>>>} - A promise that resolves with a list of matches matching the filter.
   */
  getMatchByFilter: async (
    filter: MatchFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<MatchResponse>>> =>
    await sendGet<GenericResponsePagination<MatchResponse>>(
      routes.matches,
      filter
    ),

  /**
   * Deletes a match by its ID.
   * @param {string} id - The ID of the match to delete.
   * @returns {Promise<AxiosResponse<MatchResponse>>} - A promise that resolves when the match is deleted.
   */
  deleteMatchById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete<void>(`${routes.matches}/${id}`),
};
