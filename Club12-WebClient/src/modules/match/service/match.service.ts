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
  IAddMatchRequest,
  MatchFiltered,
  IMatchResponse,
  IMinimalMatchResponse,
  IPutMatchRequest,
  IPutMatchScoreRequest,
} from '@/modules/match/type/match';

/**
 * MatchService provides methods to interact with the matches API.
 */
export const matchService = {
  /**
   * Adds a new match.
   * @param {IAddMatchRequest} match - The match data to be added.
   * @returns {Promise<AxiosResponse<IMinimalMatchResponse>>} - A promise that resolves with the server response.
   */
  addMatch: async (
    match: IAddMatchRequest
  ): Promise<AxiosResponse<IMinimalMatchResponse>> =>
    sendPost<IMinimalMatchResponse>(routes.matches, match),

  /**
   * Updates the score of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {IPutMatchScoreRequest} matchScore - The new match score data.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the server response.
   */
  putMatchScoreByMatchId: async (
    id: GUID,
    matchScore: IPutMatchScoreRequest
  ): Promise<AxiosResponse<IMatchResponse>> =>
    sendPut<IMatchResponse>(`${routes.matches}/${id}/score`, matchScore),

  /**
   * Updates the date of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {IPutMatchRequest} matchDate - The new match date data.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the server response.
   */
  putMatchByMatchId: async (
    id: GUID,
    matchDate: IPutMatchRequest
  ): Promise<AxiosResponse<IMatchResponse>> =>
    sendPut<IMatchResponse>(`${routes.matches}/${id}`, matchDate),

  /**
   * Retrieves a match by its ID.
   * @param {string} id - The ID of the match to retrieve.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the match data.
   */
  getMatchById: async (id: GUID): Promise<AxiosResponse<IMatchResponse>> =>
    sendGet<IMatchResponse>(`${routes.matches}/${id}`),

  /**
   * Retrieves matches based on the provided filter.
   * @param {MatchFiltered} filter - The filter to apply when retrieving matches.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IMatchResponse>>>} - A promise that resolves with a list of matches matching the filter.
   */
  getMatchByFilter: async (
    filter: MatchFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IMatchResponse>>> =>
    sendGet<GenericResponsePagination<IMatchResponse>>(
      routes.matches,
      withTablePageSize(filter)
    ),

  /**
   * Deletes a match by its ID.
   * @param {string} id - The ID of the match to delete.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves when the match is deleted.
   */
  deleteMatchById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.matches}/${id}`),
};
