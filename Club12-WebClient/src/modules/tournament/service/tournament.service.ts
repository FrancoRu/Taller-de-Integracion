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
  IAddTournamentRequest,
  IEnrollTeamRequest,
  IPutTournamentRequest,
  ITournamentFiltered,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament.d';

/**
 * Service for managing tournaments.
 */
export const tournamentService = {
  /**
   * Adds a new tournament.
   * @param {IAddTournamentRequest} tournament - The tournament details to add.
   * @returns {Promise<AxiosResponse<ITournamentResponse>>} The server response.
   */
  addTournament: async (
    tournament: IAddTournamentRequest
  ): Promise<AxiosResponse<ITournamentResponse>> =>
    await sendPost(`${routes.tournaments}`, tournament),

  /**
   * Updates an existing tournament.
   * @param {string} id - The ID of the tournament to update.
   * @param {IPutTournamentRequest} tournament - The updated tournament details.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putTournamentById: async (
    id: GUID,
    tournament: IPutTournamentRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPut<void>(`${routes.tournaments}/${id}`, tournament),

  /**
   * Retrieves a tournament by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the tournament to retrieve.
   * @returns {Promise<AxiosResponse<ITournamentResponse>>} The server response containing the tournament details.
   */
  getTournamentById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<ITournamentResponse>> =>
    await sendGet(`${routes.tournaments}/${idOrSlug}`),

  /**
   * Retrieves tournaments based on the provided filters.
   * @param {ITournamentFiltered} filter - The filters to apply when retrieving tournaments.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<ITournamentResponse>>>} The server response containing the filtered tournaments.
   */
  getAllTournamentsByFilter: async (
    filter: ITournamentFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<ITournamentResponse>>> =>
    await sendGet(routes.tournaments, withTablePageSize(filter)),

  /**
   * Deletes a tournament by its ID.
   * @param {string} id - The ID of the tournament to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteTournamentById: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.tournaments}/${id}`),

  /**
   * Registers one or more teams in a specific tournament.
   *
   * @async
   * @function registerTeams
   * @param {string} id - The unique identifier (GUID) of the tournament where teams will be registered.
   * @param {string[]} teamsId - An array of team identifiers (GUIDs) to be registered in the tournament.
   * @returns {Promise<AxiosResponse<boolean>>} A promise resolving to an Axios response indicating whether the registration was successful.
   */
  registerTeamsByTournamentId: async (
    id: GUID,
    teamsId: GUID[]
  ): Promise<AxiosResponse<boolean>> =>
    await sendPost(`${routes.tournaments}/register-teams/${id}`, {
      teamIds: teamsId,
    }),

  /**
   * Enrolls a single team into a tournament during its registration phase
   * (HU-107). Sends the enrollment contract to
   * `POST /api/tournaments/{id}/enroll-team`.
   *
   * @async
   * @function enrollTeam
   * @param {GUID} id - The tournament identifier (GUID) to enroll the team into.
   * @param {IEnrollTeamRequest} request - Enrollment payload. Exactly one of
   *   `existingTeamId` / `newTeamName` must be set; `copyRosterFromTournamentId`
   *   optionally seeds the roster from a previous season.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  enrollTeam: async (
    id: GUID,
    request: IEnrollTeamRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPost(`${routes.tournaments}/${id}/enroll-team`, request),
};
