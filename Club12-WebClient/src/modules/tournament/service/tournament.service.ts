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
  ITournamentCompletability,
  ITournamentFiltered,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament.d';
import {
  ICreateFullDivisionRequest,
  ICreateFullTournamentRequest,
} from '@/modules/tournament/type/createFullTournament.d';
import { IDivisionResponse } from '@/modules/division/type/division.d';

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
   * HU-38: creates a WHOLE tournament — base fields plus every division
   * (zone/cross-cup) with its points, cups, playoff mappings and stages — in a
   * single atomic transaction via `POST /api/tournaments/full`. A failure at any
   * step persists nothing (all-or-nothing), and the backend creates the
   * tournament already `OpenForRegistration`, so no separate open-registration
   * call is needed.
   * @param {ICreateFullTournamentRequest} request - The full tournament-wizard payload.
   * @returns {Promise<AxiosResponse<ITournamentResponse>>} The created tournament (with its divisions).
   */
  createFullTournament: async (
    request: ICreateFullTournamentRequest
  ): Promise<AxiosResponse<ITournamentResponse>> =>
    await sendPost(`${routes.tournaments}/full`, request),

  /**
   * HU-31/HU-112: adds ONE division (group stage + cups + playoff mappings) to
   * an already-existing tournament in a single atomic transaction via
   * `POST /api/tournaments/{tournamentId}/divisions/full` — the same structure
   * guarantee a wizard-created division gets, instead of the bare division the
   * granular `POST /api/divisions` endpoint leaves behind (no stages, no cups).
   * @param {GUID} tournamentId - The parent tournament's id.
   * @param {ICreateFullDivisionRequest} request - The division's structure (zone or cross-cup).
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} The created division.
   */
  addFullDivision: async (
    tournamentId: GUID,
    request: ICreateFullDivisionRequest
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    await sendPost(`${routes.tournaments}/${tournamentId}/divisions/full`, request),

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

  /**
   * Removes a team's enrollment from a tournament (HU-108). Sends
   * `DELETE /api/tournaments/{id}/teams/{teamId}`. The backend answers 204 on
   * success and 409 once the tournament has started.
   *
   * @async
   * @function unenrollTeam
   * @param {GUID} id - The tournament identifier (GUID).
   * @param {GUID} teamId - The team identifier (GUID) to unenroll.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  unenrollTeam: async (
    id: GUID,
    teamId: GUID
  ): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.tournaments}/${id}/teams/${teamId}`),

  /**
   * Fetches the live completability report for a tournament (HU-109) from
   * `GET /api/tournaments/{id}/completability`.
   *
   * @async
   * @function getCompletability
   * @param {GUID} id - The tournament identifier (GUID).
   * @returns {Promise<AxiosResponse<ITournamentCompletability>>} The server response.
   */
  getCompletability: async (
    id: GUID
  ): Promise<AxiosResponse<ITournamentCompletability>> =>
    await sendGet(`${routes.tournaments}/${id}/completability`),
};
