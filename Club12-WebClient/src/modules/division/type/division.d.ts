import {
  FetchOptions,
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { ITeamResponse } from '@/modules/team/type/team.d';

/**
 * Context properties and methods for managing divisions in a React application.
 * These methods interact with the backend for creating, updating, fetching, and deleting divisions.
 * @interface IDivisionContextProps
 */
export interface IDivisionContextProps {
  division: IDivisionResponse | null;
  divisions: IDivisionResponse[] | null;
  /**
   * Adds a new division to the system.
   * @param division The details of the division to add.
   * @returns A promise that resolves with the response containing the newly added division.
   */
  addDivision(division: AddDivisionRequest): Promise<IDivisionResponse | void>;

  /**
   * Generates fixtures for a division based on its ID.
   * @param id The ID of the division for which to generate fixtures.
   * @returns A promise that resolves when the fixtures are successfully generated.
   */
  generateFixtureByDivisionId(id: GUID): Promise<void>;

  /**
   * Updates an existing division by its ID.
   * @param id The ID of the division to update.
   * @param division The updated division data.
   * @returns A promise that resolves with the response containing the updated division.
   */
  putDivisionById(
    id: GUID,
    division: IPutDivisionRequest
  ): Promise<boolean | void>;

  /**
   * Fetches a division by its ID or its public slug.
   * @param idOrSlug The ID or slug of the division to fetch.
   * @returns A promise that resolves with the division data.
   */
  getDivisionsById(idOrSlug: string): Promise<IDivisionResponse | void>;

  /**
   * Fetches divisions based on filters and pagination.
   * @param filter The filter criteria to apply when fetching divisions.
   * @returns A promise that resolves with a paginated response containing filtered divisions.
   */
  getDivisionsByFilters(
    filter: DivisionFiltered,
    options?: FetchOptions
  ): Promise<GenericResponsePagination<IDivisionResponse> | void>;

  /**
   * Deletes a division by its ID.
   * @param id The ID of the division to delete.
   * @returns A promise resolving to `true` if the division was deleted,
   * `false` if the request failed (the global error is already reported
   * either way).
   */
  deleteDivisionsById(id: GUID): Promise<boolean>;

  /**
   * Fetches every team currently enrolled in a division's roster
   * (`DivisionTeamRegistration`), independent of any stage placement. The
   * authoritative source of "who is in this division" — including a
   * playoffs-only division with no group stage.
   * @param divisionId The division whose roster to fetch.
   * @returns A promise resolving to the enrolled teams, or void on failure.
   */
  getRoster(divisionId: GUID): Promise<ITeamResponse[] | void>;

  /**
   * Enrols one or more teams in a division's roster. Rejected (409) when a
   * team already holds a registration in another regular division of the
   * same tournament, or the tournament structure is locked.
   * @param divisionId The division to enrol the teams into.
   * @param teamIds The teams to enrol.
   * @returns A promise resolving to true on success, or void on failure.
   */
  enrollTeams(divisionId: GUID, teamIds: GUID[]): Promise<boolean | void>;

  /**
   * Removes one or more teams from a division's roster. Cascades: any
   * `StageTeamMatch` the team still holds within the division's stages is
   * removed in the same operation.
   * @param divisionId The division to unenrol the teams from.
   * @param teamIds The teams to unenrol.
   * @returns A promise resolving to true on success, or void on failure.
   */
  unenrollTeams(divisionId: GUID, teamIds: GUID[]): Promise<boolean | void>;

  /**
   * Clears the division's current sub-group placements and re-runs a
   * balanced random distribution over its whole roster (HU-122). Always
   * balanced, not fill-only-empties.
   * @param divisionId The division whose sub-groups to auto-distribute.
   * @returns A promise resolving to true on success, or void on failure.
   */
  autoDistribute(divisionId: GUID): Promise<boolean | void>;

  /**
   * Rebuilds a division's sub-group stage layer to a new count, keeping the
   * roster untouched, and re-runs the balanced distribution over it (HU-123).
   * @param divisionId The division whose sub-group count to change.
   * @param subGroupCount The new sub-group count.
   * @returns A promise resolving to true on success, or void on failure.
   */
  rebuildSubGroups(divisionId: GUID, subGroupCount: number): Promise<boolean | void>;

  /**
   * Manually moves one enrolled team from one sub-group to another within the
   * same division (HU-122), without touching any other team's placement.
   * Rejected (409) when the move would drop the source sub-group below the
   * minimum size, the team is not currently placed in `fromStageId`, or the
   * two stages belong to different divisions.
   * @param divisionId The division the two sub-groups belong to.
   * @param teamId The team to move.
   * @param fromStageId The sub-group stage the team currently belongs to.
   * @param toStageId The sub-group stage to move the team into.
   * @returns A promise resolving to true on success, or void on failure.
   */
  reassignTeamToSubGroup(
    divisionId: GUID,
    teamId: GUID,
    fromStageId: GUID,
    toStageId: GUID
  ): Promise<boolean | void>;
}

/**
 * One position-range → playoff-destination entry (HU-45) sent with a
 * division so the backend can seed multiple cups from the final table
 * (HU-81). Field names mirror the backend `PlayoffMappingRequest` DTO.
 * @interface PlayoffMappingRequest
 */
export interface PlayoffMappingRequest {
  /** First standings position in the range (1-based, inclusive). */
  fromPosition: number;

  /** Last standings position in the range (1-based, inclusive). */
  toPosition: number;

  /** The destination cup's BracketName (e.g. "Copa Oro"). */
  destination: string;
}

/**
 * The request body structure for adding a new division.
 * @interface AddDivisionRequest
 */
export interface AddDivisionRequest {
  /**
   * The name of the division.
   * @type {string}
   */
  name: string;

  /**
   * The ID of the tournament to which the division belongs.
   * @type {GUID}
   */
  tournamentId: GUID;

  /**
   * Marks this division as a cross-division cup that intentionally draws
   * teams from every other division in the tournament (e.g. an
   * admin-named "Copa Club12"), exempt from the "one team, one division"
   * rule. Defaults to false.
   * @type {boolean}
   */
  isCrossDivisionCup?: boolean;

  /**
   * For a cross-division cup (HU-110): how many teams advance from each of
   * the cup's group stages into the pooled knockout bracket. Only meaningful
   * when `isCrossDivisionCup` is true; the backend auto-sizes the bracket's
   * first round from the pooled top-`qualifiersPerGroup` of every group.
   * @type {number}
   */
  qualifiersPerGroup?: number;

  /**
   * Points awarded for a win in this division's standings (HU-79).
   * Omit to let the backend default to 2.
   * @type {number}
   */
  pointsForWin?: number;

  /**
   * Points awarded for a loss in this division's standings (HU-79).
   * Omit to let the backend default to 1.
   * @type {number}
   */
  pointsForLoss?: number;

  /**
   * Competitive category (gender) of the division (HU-48). MUST match the
   * parent tournament's category — the backend rejects a division whose
   * category differs from its tournament, and `Division.Category` defaults to
   * Masculine server-side. The wizard therefore sends the tournament's
   * category on every division so a Feminine tournament's zones are created
   * as Feminine and not rejected.
   * @type {TournamentCategory}
   */
  category?: TournamentCategory;

  /**
   * Optional position-range → playoff-destination mappings (HU-45) the
   * wizard sends so the backend can seed multiple cups (HU-81). Ranges
   * must not overlap.
   * @type {PlayoffMappingRequest[]}
   */
  playoffMappings?: PlayoffMappingRequest[];
}

/**
 * One standings-position range that qualifies to a playoff cup (HU-45),
 * shaped for the public standings table so it can highlight the qualifying
 * rows and render a per-cup legend. Mirrors the backend
 * `QualificationRangeResponse` DTO.
 * @interface QualificationRange
 */
export interface QualificationRange {
  /** First standings position in the range (1-based, inclusive). */
  fromPosition: number;

  /** Last standings position in the range (1-based, inclusive). */
  toPosition: number;

  /** The cup the teams in this range qualify for (e.g. "Copa Oro"). */
  cupName: string;

  /**
   * The cup's rank, top-down: 0 is the top cup ("Copa Oro"), 1 the next, and
   * so on. Drives the color painted on each qualifying row.
   */
  order: number;
}

/**
 * The response structure for a division, including details about the division, its matches, and positions.
 * @interface IDivisionResponse
 */
export interface IDivisionResponse {
  /**
   * The unique identifier of the division.
   * @type {GUID}
   */
  id: GUID;

  /**
   * The name of the division.
   * @type {string}
   */
  name: string;

  /**
   * The unique, URL-friendly identifier used in public division links.
   * @type {string}
   */
  slug: string;

  /**
   * Indicates whether the division has finished.
   * @type {boolean}
   */
  isFinished: boolean;

  /**
   * The list of positions for teams in the division. For a multi-group
   * cross-division cup this is the pooled union across every internal group
   * (so a team counter reflects all groups); use `groupStandings` to render
   * one table per group.
   * @type {Position[]}
   */
  positions?: Position[];

  /**
   * One standings table per Group stage (HU-110). A regular zone has a single
   * entry; a multi-group cross-division cup has one per internal group
   * ("Grupo 1".."Grupo N"). Absent/empty when the division has no group stage.
   * @type {GroupStandings[]}
   */
  groupStandings?: GroupStandings[];

  /**
   * The ID of the tournament to which the division belongs.
   * @type {GUID}
   */
  tournamentId: GUID;

  /**
   * The parent tournament's slug, when it was resolved by the backend; null
   * otherwise. Prefer this over `tournamentId` when building a link back to
   * the tournament, so the URL never shows a raw UUID.
   * @type {string | null}
   */
  tournamentSlug?: string | null;

  /**
   * Whether this division is a cross-division cup (exempt from the "one
   * team, one division" rule).
   * @type {boolean}
   */
  isCrossDivisionCup: boolean;

  /**
   * For a cross-division cup (HU-110): how many teams advance from EACH of
   * the cup's internal groups into the pooled knockout bracket. Meaningless
   * (defaults to 1) outside a cross-division cup.
   * @type {number}
   */
  qualifiersPerGroup?: number;

  /**
   * Competitive category (gender) of the division — matches its tournament.
   * Used to tell apart same-named zones across masculine/feminine tournaments.
   * @type {TournamentCategory}
   */
  category?: TournamentCategory;

  /**
   * The standings-position ranges that qualify to a playoff cup (HU-45),
   * ordered top-down (order 0 = top cup). Lets the public standings table
   * highlight the qualifying rows and render a per-cup legend. Absent/empty
   * when the division has no playoff mappings.
   * @type {QualificationRange[]}
   */
  qualificationRanges?: QualificationRange[];
}

/**
 * Standings for a single Group stage within a division. A regular zone has
 * exactly one; a multi-group cross-division cup (HU-110) has one per internal
 * group, each computed only over that group's own matches.
 * @type GroupStandings
 */
export type GroupStandings = {
  /** The id of the Group stage these standings belong to. */
  stageId: GUID;

  /** The Group stage's name, used as the table label (e.g. "Grupo 1"). */
  stageName: string;

  /** The ordered standings for the teams in this group. */
  positions: Position[];
};

/**
 * The structure for a position in a division, including team statistics.
 * @type Position
 */
export type Position = {
  /**
   * The unique identifier of the team.
   * @type {string}
   */
  teamId: GUID;

  /**
   * The name of the team.
   * @type {string}
   */
  teamName: string;

  /**
   * The URL of the team's logo.
   * @type {string}
   */
  logoUrl: string;

  /**
   * The number of matches the team has played.
   * @type {number}
   */
  matchesPlayed: number;

  /**
   * The number of matches the team has won.
   * @type {number}
   */
  wins: number;

  /**
   * The number of matches the team has lost.
   * @type {number}
   */
  losses: number;

  /**
   * The number of points the team has scored.
   * @type {number}
   */
  pointsFor: number;

  /**
   * The number of points scored against the team.
   * @type {number}
   */
  pointsAgainst: number;

  /**
   * The difference between points scored and points against.
   * @type {number}
   */
  pointsDifference: number;

  /**
   * The total points the team has earned. Any disciplinary deduction
   * (see `pointDeduction`) is already subtracted from this value.
   * @type {number}
   */
  points: number;

  /**
   * The disciplinary point deduction (deducción de puntos) applied to this
   * team, when any. Absent when the team has no deduction. The subtraction is
   * already reflected in `points`; this only carries the amount and reason so
   * the standings can show a subtle "-N (motivo)" note.
   */
  pointDeduction?: AppliedPointDeduction;
};

/**
 * The point-deduction summary attached to a standings row when a team carries
 * one or more disciplinary deductions. Mirrors the backend
 * `AppliedPointDeductionResponse` DTO.
 * @type AppliedPointDeduction
 */
export type AppliedPointDeduction = {
  /** The total table points subtracted from the team. Always positive. */
  points: number;

  /** The combined disciplinary reason(s). */
  reason: string;
};

/**
 * The filter criteria for fetching divisions, which extends from PutDivisionRequest and Filtered.
 * This includes the `isFinished` property to filter divisions by their completion status.
 * @interface DivisionFiltered
 * @extends IPutDivisionRequest
 * @extends Filtered
 */
export interface DivisionFiltered extends Filtered {
  /**
   * Indicates whether to fetch finished divisions only.
   * @type {boolean}
   */
  isFinished?: boolean;

  tournamentId?: GUID;

  /**
   * The updated name of the division.
   * @type {string}
   */
  name?: string;
}

/**
 * The request body structure for updating an existing division.
 * @interface PutDivisionRequest
 */
export interface IPutDivisionRequest {
  /**
   * The updated name of the division.
   * @type {string}
   */
  name: string;

  isFinished: boolean;
}

export interface IDivisionPropsView {
  name: string;
}

/**
 * The request body to enrol teams in a division's roster.
 * @interface EnrollTeamsRequest
 */
export interface EnrollTeamsRequest {
  teamIds: GUID[];
}

/**
 * The request body to remove teams from a division's roster.
 * @interface UnenrollTeamsRequest
 */
export interface UnenrollTeamsRequest {
  teamIds: GUID[];
}

/**
 * The request body to change a division's sub-group count (HU-123).
 * @interface RebuildSubGroupsRequest
 */
export interface RebuildSubGroupsRequest {
  subGroupCount: number;
}

/**
 * The request body to manually move one team from one sub-group to another
 * within the same division (HU-122).
 * @interface ReassignTeamToSubGroupRequest
 */
export interface ReassignTeamToSubGroupRequest {
  teamId: GUID;
  fromStageId: GUID;
  toStageId: GUID;
}

/**
 * The minimal response structure for a division, as embedded within a tournament response.
 * @interface IMinimalDivisionResponse
 */
export interface IMinimalDivisionResponse {
  /**
   * The unique identifier of the division.
   * @type {GUID}
   */
  id: GUID;

  /**
   * The name of the division.
   * @type {string}
   */
  name: string;

  /**
   * Indicates whether the division has finished.
   * @type {boolean}
   */
  isFinished: boolean;
}
