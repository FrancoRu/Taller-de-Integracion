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
  IAddMatchRequest,
  MatchFiltered,
  IMatchResponse,
  PutMatchDateRequest,
  PutMatchScoreRequest,
} from '../type/match';

/**
 * MatchService provides methods to interact with the matches API.
 */
export const matchService = {
  /**
   * Adds a new match.
   * @param {IAddMatchRequest} match - The match data to be added.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the server response.
   */
  addMatch: async (
    match: IAddMatchRequest
  ): Promise<AxiosResponse<IMatchResponse>> =>
    sendPost<IMatchResponse>(routes.matches, match),

  /**
   * Updates the score of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {PutMatchScoreRequest} matchScore - The new match score data.
   * @returns {Promise<AxiosResponse<void>>} - A promise that resolves with the server response.
   */
  putMatchScoreByMatchId: async (
    id: GUID,
    matchScore: PutMatchScoreRequest
  ): Promise<AxiosResponse<void>> =>
    sendPut<void>(`${routes.matches}/${id}/score`, matchScore),

  /**
   * Updates the date of an existing match.
   * @param {string} id - The ID of the match to update.
   * @param {PutMatchDateRequest} matchDate - The new match date data.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the server response.
   */
  putMatchDateByMatchId: async (
    id: GUID,
    matchDate: PutMatchDateRequest
  ): Promise<AxiosResponse<void>> =>
    sendPut<void>(`${routes.matches}/${id}/date`, matchDate),

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
    sendGet<GenericResponsePagination<IMatchResponse>>(routes.matches, filter),

  /**
   * Deletes a match by its ID.
   * @param {string} id - The ID of the match to delete.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves when the match is deleted.
   */
  deleteMatchById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.matches}/${id}`),
};
