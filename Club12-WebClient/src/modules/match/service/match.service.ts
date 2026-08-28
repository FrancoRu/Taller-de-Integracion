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
  ILoadWalkOverRequest,
  MatchFiltered,
  IMatchResponse,
  IMinimalMatchResponse,
  IPutMatchRequest,
  IPutMatchScoreRequest,
  IRoundMatchesResponse,
  ISuspendMatchRequest,
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
   * Marks a match as a walkover (HU-73), awarding the regulation default
   * result to the present team.
   * @param {string} id - The ID of the match to mark as a walkover.
   * @param {ILoadWalkOverRequest} request - The present team (and optional score override).
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the updated match.
   */
  loadWalkOver: async (
    id: GUID,
    request: ILoadWalkOverRequest
  ): Promise<AxiosResponse<IMatchResponse>> =>
    sendPut<IMatchResponse>(`${routes.matches}/${id}/walkover`, request),

  /**
   * Retrieves a match by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the match to retrieve.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the match data.
   */
  getMatchById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IMatchResponse>> =>
    sendGet<IMatchResponse>(`${routes.matches}/${idOrSlug}`),

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
   * Retrieves a stage's matches grouped and ordered by matchday (jornada,
   * HU-63) so the fixture renders as "Fecha 1 / Partido 1..2, Fecha 2 / …".
   * @param {GUID} stageId - The ID of the stage whose fixture is requested.
   * @returns {Promise<AxiosResponse<IRoundMatchesResponse[]>>} - A promise that resolves with the rounds.
   */
  getStageMatchesByRound: async (
    stageId: GUID
  ): Promise<AxiosResponse<IRoundMatchesResponse[]>> =>
    sendGet<IRoundMatchesResponse[]>(
      `${routes.matches}/stage/${stageId}/by-round`
    ),

  /**
   * Reprograms/suspends a match (HU-68): marks it suspended and optionally
   * moves it to a new date, without changing its round (HU-67).
   * @param {GUID} id - The ID of the match to suspend/reprogram.
   * @param {ISuspendMatchRequest} request - The optional new date.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves with the updated match.
   */
  suspendMatch: async (
    id: GUID,
    request: ISuspendMatchRequest
  ): Promise<AxiosResponse<IMatchResponse>> =>
    sendPut<IMatchResponse>(`${routes.matches}/${id}/suspend`, request),

  /**
   * Deletes a match by its ID.
   * @param {string} id - The ID of the match to delete.
   * @returns {Promise<AxiosResponse<IMatchResponse>>} - A promise that resolves when the match is deleted.
   */
  deleteMatchById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.matches}/${id}`),

  /**
   * Automatically generates matches for a stage (e.g. a round-robin group
   * fixture, or the empty slots of an elimination round).
   * @param {string} id - The ID of the stage to generate matches for.
   * @returns {Promise<AxiosResponse<IMatchResponse[]>>} - A promise that resolves with the generated matches.
   */
  generateMatches: async (
    id: GUID
  ): Promise<AxiosResponse<IMatchResponse[]>> =>
    sendPost<IMatchResponse[]>(`${routes.matches}/generate/${id}`),
};
