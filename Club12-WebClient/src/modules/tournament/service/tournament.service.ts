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
  AddTournamentRequest,
  PutTournamentRequest,
  TournamentFiltered,
  TournamentResponse,
} from "../type/tournament";

/**
 * Service for managing tournaments.
 */
export const tournamentService = {
  /**
   * Adds a new tournament.
   * @param {AddTournamentRequest} tournament - The tournament details to add.
   * @returns {Promise<AxiosResponse<TournamentResponse>>} The server response.
   */
  addTournament: async (
    tournament: AddTournamentRequest
  ): Promise<AxiosResponse<TournamentResponse>> =>
    await sendPost(`${routes.tournaments}`, tournament),

  /**
   * Updates an existing tournament.
   * @param {string} id - The ID of the tournament to update.
   * @param {PutTournamentRequest} tournament - The updated tournament details.
   * @returns {Promise<AxiosResponse<TournamentResponse>>} The server response.
   */
  putTournamentById: async (
    id: string,
    tournament: PutTournamentRequest
  ): Promise<AxiosResponse<TournamentResponse>> =>
    await sendPut(`${routes.tournaments}/${id}`, tournament),

  /**
   * Retrieves a tournament by its ID.
   * @param {string} id - The ID of the tournament to retrieve.
   * @returns {Promise<AxiosResponse<TournamentResponse>>} The server response containing the tournament details.
   */
  getTournamentById: async (
    id: string
  ): Promise<AxiosResponse<TournamentResponse>> =>
    await sendGet(`${routes.tournaments}/${id}`),

  /**
   * Retrieves tournaments based on the provided filters.
   * @param {TournamentFiltered} filter - The filters to apply when retrieving tournaments.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<TournamentResponse>>>} The server response containing the filtered tournaments.
   */
  getAllTournamentsByFilter: async (
    filter: TournamentFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<TournamentResponse>>> =>
    await sendGet(routes.tournaments, filter),

  /**
   * Deletes a tournament by its ID.
   * @param {string} id - The ID of the tournament to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteTournamentById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.tournaments}/${id}`),
};
