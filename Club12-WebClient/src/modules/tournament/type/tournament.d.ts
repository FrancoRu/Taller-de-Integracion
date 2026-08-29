import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  FetchOptions,
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
  getTournamentById(
    idOrSlug: string,
    options?: FetchOptions
  ): Promise<ITournamentResponse | void>;

  /**
   * Fetches tournaments based on filters.
   * @param filter The filters to apply when fetching tournaments.
   * @param options Per-call options; `silent` suppresses the global alert on failure.
   * @returns A promise that resolves with the paginated response containing tournaments that match the filters.
   */
  getAllTournamentsByFilter(
    filter: ITournamentFiltered,
    options?: FetchOptions
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

  /**
   * Enrolls a single team into a tournament during its registration phase
   * (HU-107). Either an existing team is enrolled (existingTeamId) or a brand
   * new team is created and enrolled in one step (newTeamName) — exactly one of
   * the two. When an existing team is enrolled, its roster can be seeded from a
   * previous season by passing copyRosterFromTournamentId.
   * @param id The tournament identifier (GUID) to enroll the team into.
   * @param request The enrollment payload (existing team or new team + optional roster copy).
   * @returns A promise resolving to `true` on success, otherwise void.
   */
  enrollTeam(
    id: GUID,
    request: IEnrollTeamRequest
  ): Promise<boolean | void>;

  /**
   * Removes a team's enrollment from a tournament during its registration
   * phase (HU-108). Hits `DELETE /api/tournaments/{id}/teams/{teamId}`, which
   * the backend rejects with 409 once the tournament has started.
   * @param id The tournament identifier (GUID) to unenroll the team from.
   * @param teamId The team identifier (GUID) to unenroll.
   * @returns A promise resolving to `true` on success, otherwise void.
   */
  unenrollTeam(id: GUID, teamId: GUID): Promise<boolean | void>;

  /**
   * Fetches the live completability report for a tournament (HU-109) from
   * `GET /api/tournaments/{id}/completability`. Reports whether the tournament
   * can start and, if not, the list of blocking issues.
   * @param id The tournament identifier (GUID).
   * @returns A promise resolving to the completability report, or void on failure.
   */
  getCompletability(id: GUID): Promise<ITournamentCompletability | void>;
}

/**
 * A single blocking issue preventing a tournament from starting (HU-109), as
 * returned by the completability endpoint. `code` is a stable machine-readable
 * identifier; the optional fields carry the context each code needs to build a
 * human-readable message.
 * @interface ICompletabilityIssue
 */
export interface ICompletabilityIssue {
  /**
   * Stable issue code (e.g. `ZoneTooFewTeams`, `TeamNotAssigned`,
   * `TeamInMultipleZones`, `PlayoffRangeExceedsTeams`,
   * `CrossCupGroupTooFewTeams`).
   * @type {string}
   */
  code: string;

  /** Name of the division/zone the issue refers to, when applicable. */
  divisionName?: string;

  /** Name of the team the issue refers to, when applicable. */
  teamName?: string;

  /** First standings position of the offending playoff range, when applicable. */
  fromPosition?: number;

  /** Number of teams assigned to the offending zone/group, when applicable. */
  assignedTeams?: number;
}

/**
 * The live completability report for a tournament (HU-109).
 * @interface ITournamentCompletability
 */
export interface ITournamentCompletability {
  /** Whether the tournament can transition to Ongoing (start). */
  canStart: boolean;

  /** The blocking issues; empty when `canStart` is true. */
  issues: ICompletabilityIssue[];
}

/**
 * The request body structure for enrolling a team into a tournament (HU-107).
 * Exactly one of `existingTeamId` / `newTeamName` must be provided.
 * @interface IEnrollTeamRequest
 */
export interface IEnrollTeamRequest {
  /**
   * The identifier of an already-existing team (club) to enroll.
   * @type {GUID}
   */
  existingTeamId?: GUID;

  /**
   * The name of a brand-new team to create and enroll in one step.
   * @type {string}
   */
  newTeamName?: string;

  /**
   * When enrolling an existing team, the tournament (season) whose roster
   * should be copied as the starting plantel for this enrollment. Typically the
   * team's most recent season.
   * @type {GUID}
   */
  copyRosterFromTournamentId?: GUID;
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

  /**
   * Competitive category (gender) of the tournament (HU-48). Chosen at
   * creation and immutable afterwards. The feminine competition is played as
   * its own separate tournament. Omit to let the backend default to
   * Masculine.
   * @type {TournamentCategory}
   */
  category?: TournamentCategory;

  /**
   * Optional season ("Temporada") the tournament is grouped under. Omit to
   * leave the tournament without a season.
   * @type {GUID}
   */
  seasonId?: GUID | null;
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

  /**
   * Competitive category (gender) of the tournament (HU-48). The feminine
   * competition is played as its own separate tournament.
   * @type {TournamentCategory}
   */
  category: TournamentCategory;

  /**
   * The season ("Temporada") the tournament is grouped under, or `null` when
   * it belongs to no season.
   * @type {GUID}
   */
  seasonId: GUID | null;

  /**
   * The name of the season the tournament belongs to, or `null` when it
   * belongs to no season.
   * @type {string}
   */
  seasonName: string | null;
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

  /**
   * Optional season ("Temporada") the tournament is grouped under. Send `null`
   * to detach the tournament from its season.
   * @type {GUID}
   */
  seasonId?: GUID | null;
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
