import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { IMinimalDivisionResponse } from '@/modules/division/type/division';

/**
 * Context properties and methods for managing tournaments.
 * These methods allow for creating, updating, fetching, and deleting tournaments.
 * @interface ITournamentContextProps
 */
export interface ITournamentContextProps {
  tournament: ITournamentResponse | null;
  tournaments: ITournamentResponse[] | null;

  /**
   * Adds a new tournament.
   * @param tournament The details of the tournament to add.
   * @returns A promise that resolves with the response containing the newly added tournament.
   */
  addTournament(
    tournament: IAddTournamentRequest
  ): Promise<ITournamentResponse | void>;

  /**
   * Updates an existing tournament by its ID.
   * @param id The ID of the tournament to update.
   * @param tournament The updated tournament data.
   * @returns A promise that resolves when the tournament is successfully updated.
   */
  putTournamentById(id: GUID, tournament: IPutTournamentRequest): Promise<void>;

  /**
   * Fetches a tournament by its ID or its public slug.
   * @param idOrSlug The ID or slug of the tournament to fetch.
   * @returns A promise that resolves with the tournament details.
   */
  getTournamentById(idOrSlug: string): Promise<ITournamentResponse | void>;

  /**
   * Fetches tournaments based on filters.
   * @param filter The filters to apply when fetching tournaments.
   * @returns A promise that resolves with the paginated response containing tournaments that match the filters.
   */
  getAllTournamentsByFilter(
    filter: ITournamentFiltered
  ): Promise<GenericResponsePagination<ITournamentResponse> | void>;

  /**
   * Deletes a tournament by its ID.
   * @param id The ID of the tournament to delete.
   * @returns A promise that resolves when the tournament is successfully deleted.
   */
  deleteTournamentById(id: GUID): Promise<void>;

  /**
   * Registers one or more teams in a specific tournament.
   *
   * @async
   * @function registerTeams
   * @param {string} id - The unique identifier (GUID) of the tournament where teams will be registered.
   * @param {string[]} teamsId - An array of team identifiers (GUIDs) to be registered in the tournament.
   * @returns {Promise<AxiosResponse<boolean>>} A promise resolving to an Axios response indicating whether the registration was successful.
   */
  registerTeamsByTournamentId(
    id: GUID,
    teamsId: GUID[]
  ): Promise<boolean | void>;
}

/**
 * The request body structure for adding a new tournament.
 * @interface IAddTournamentRequest
 */
export interface IAddTournamentRequest {
  /**
   * The name of the tournament.
   * @type {string}
   */
  name: string;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description: string;

  /**
   * The deadline for team registrations.
   * Must be earlier than the tournament start date.
   * @type {Date}
   */
  teamRegistrationDeadline: Date;

  /**
   * The start date of the tournament.
   * @type {Date}
   */
  startDate: Date;
}

/**
 * The response structure for a tournament.
 * @interface ITournamentResponse
 */
export interface ITournamentResponse {
  /**
   * The unique ID of the tournament.
   * @type {GUID}
   */
  id: GUID;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description: string;

  /**
   * The name of the tournament.
   * @type {string}
   */
  name: string;

  /**
   * The unique, URL-friendly identifier used in public tournament links.
   * @type {string}
   */
  slug: string;

  /**
   * The divisions associated with the tournament.
   * @type {IMinimalDivisionResponse[]}
   */
  divisions: IMinimalDivisionResponse[];

  /**
   * The deadline for team registrations.
   * Must be earlier than the tournament start date.
   * @type {Date}
   */
  teamRegistrationDeadline: Date;

  /**
   * The start date of the tournament.
   * @type {Date}
   */
  startDate: Date;

  /**
   * The current status of the tournament.
   * @type {TournamentStatus}
   */
  status: TournamentStatus;
}

/**
 * The structure for filtering tournaments.
 * @interface ITournamentFiltered
 */
export interface ITournamentFiltered extends Filtered {
  /**
   * The name of the tournament.
   * @type {string}
   */
  name?: string;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description?: string;

  /**
   * The current status of the tournament.
   * @type {TournamentStatus}
   */
  status?: TournamentStatus;
}

/**
 * The request body structure for updating an existing tournament.
 * @interface IPutTournamentRequest
 */
export interface IPutTournamentRequest {
  /**
   * The name of the tournament.
   * @type {string}
   */
  name: string;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description: string;

  /**
   * The deadline for team registrations.
   * Must be earlier than the tournament start date.
   * @type {Date}
   */
  teamRegistrationDeadline: Date;

  /**
   * The start date of the tournament.
   * @type {Date}
   */
  startDate: Date;

  /**
   * The current status of the tournament.
   * @type {TournamentStatus}
   */
  status?: TournamentStatus;
}

export type StatisticsPositions = {
  pj: number;
  pg: number;
  pp: number;
  gf: number;
  gc: number;
  dif: number;
  pts: number;
};

export type DataPositions = {
  id: GUID;
  nameTeam: string;
  positions: StatisticsPositions;
};
