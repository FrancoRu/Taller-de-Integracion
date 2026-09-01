import { Filtered, GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IVenueResponse } from '@/modules/venue/type/venue';
import { PlayerScoreEntry } from '@/modules/playerStatistic/type/playerStatistic';

/**
 * Context properties and methods for managing matches in a sports system.
 * These methods interact with the backend for creating, updating, fetching, and deleting matches.
 * @interface IMatchContextProps
 */
export interface IMatchContextProps {
  match: IMatchResponse | null;
  matches: IMatchResponse[] | null;
  /**
   * Adds a new match to the system.
   * @param match The details of the match to add.
   * @returns A promise that resolves with the response containing the newly added match.
   */
  addMatch(match: IAddMatchRequest): Promise<IMinimalMatchResponse | void>;

  /**
   * Updates the score of an existing match.
   * @param id The ID of the match .
   * @param matchScore The new scores for the home and visitor teams.
   * @returns A promise that resolves when the score is successfully updated.
   */
  putMatchScoreByMatchId(
    id: GUID,
    matchScore: IPutMatchScoreRequest
  ): Promise<IMatchResponse | void>;

  /**
   * Finishes a match by loading both teams' scoring sheets in one operation
   * (HU-72): the final score is derived as the sum of each team's listed
   * player points, instead of typing the score in separately.
   * @param id The ID of the match.
   * @param request Both teams' per-player points.
   * @returns A promise that resolves with the finalized match.
   */
  loadMatchResultFromSheets(
    id: GUID,
    request: IPutMatchResultFromSheetsRequest
  ): Promise<IMatchResponse | void>;

  /**
   * Updates the match date and venue.
   * @param id The ID of the match .
   * @param matchDate The new match date.
   * @returns A promise that resolves when the match date and venue are successfully updated.
   */
  putMatchByMatchId(
    id: GUID,
    matchDate: IPutMatchRequest
  ): Promise<IMatchResponse | void>;

  /**
   * Fetches a match by its ID or its public slug.
   * @param idOrSlug The ID or slug of the match to fetch.
   * @returns A promise that resolves with the match details.
   */
  getMatchById(idOrSlug: string): Promise<IMatchResponse | void>;

  /**
   * Fetches matches based on filters and pagination.
   * @param filter The filter criteria to apply when fetching matches.
   * @returns A promise that resolves with a paginated response containing filtered matches.
   */
  getMatchByFilter(
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<IMatchResponse> | void>;

  /**
   * Fetches a stage's matches grouped and ordered by matchday (jornada, HU-63)
   * so the fixture can be rendered as "Fecha 1 / Partido 1..2, Fecha 2 / …".
   * @param stageId The id of the stage whose fixture is requested.
   * @returns A promise that resolves with the rounds, or void on error.
   */
  getStageMatchesByRound(
    stageId: GUID
  ): Promise<IRoundMatchesResponse[] | void>;

  /**
   * Reprograms/suspends a match (HU-68): marks it suspended and optionally
   * moves it to a new date, without changing its round (HU-67).
   * @param id The id of the match to suspend/reprogram.
   * @param request The optional new date.
   * @returns A promise that resolves with the updated match, or void on error.
   */
  suspendMatch(
    id: GUID,
    request: ISuspendMatchRequest
  ): Promise<IMatchResponse | void>;

  /**
   * Marks a match as a walkover (HU-73), awarding the regulation default
   * result to the present team.
   * @param id The ID of the match to mark as a walkover.
   * @param request The present team (and optional score override).
   * @returns A promise that resolves with the updated match, or void on error.
   */
  loadWalkOver(
    id: GUID,
    request: ILoadWalkOverRequest
  ): Promise<IMatchResponse | void>;

  /**
   * Deletes a match by its ID.
   * @param id The ID of the match to delete.
   * @returns A promise that resolves when the match is successfully deleted.
   */
  deleteMatchById(id: GUID): Promise<void>;

  /**
   * Automatically generates matches for the specified division or tournament.
   *
   * @param {GUID} id - The unique identifier of the stage for which the stages will be generated.
   * @returns {Promise<boolean>} A promise that resolves to true if the stages were successfully generated, or false otherwise.
   */
  generateMatchesAutomatically(id: GUID): Promise<boolean>;
}

/**
 * The request body structure for adding a new match.
 * @interface AddMatchRequest
 */
export interface IAddMatchRequest {
  /**
   * The date and time of the match.
   * @type {string}
   */
  matchDate: string;

  /**
   * The type of match (e.g., Regular, Playoff).
   * @type {MatchType}
   */
  type?: MatchType;

  /**
   * The ID of the home team.
   * @type {string}
   */
  homeTeamId: GUID;

  /**
   * The ID of the visitor team.
   * @type {string}
   */
  visitorTeamId: GUID;

  /**
   * The ID of the division the match belongs to.
   * @type {GUID}
   */
  stageId: GUID;

  /**
   * The ID of the venue where the match will take place.
   * @type {GUID}
   */
  venueId?: GUID;
}

/**
 *
 */
export interface MatchFormProps<T extends IAddMatchRequest> {
  errors: string[] | null;
  startDate: string;
  endDate: string;
  form: T;
  setForm: Dispatch<SetStateAction<T>>;
}

/**
 * @interface IMatchResponse
 * @description The response structure for a match, including team details, scores, and match results.
 */
export interface IMatchResponse {
  /**
   * @property {GUID} id - The unique identifier of the match.
   */
  id: GUID;

  /**
   * @property {string} matchDate - The date and time when the match took place.
   */
  matchDate: string;

  /**
   * @property {number | null} round - The matchday (jornada) this match belongs
   * to, 1-based (HU-63/HU-65). This is the canonical grouping key for the
   * fixture ("Fecha 1", "Fecha 2", …); the UI groups by this rather than by
   * the calendar date (HU-63). Null for matches with no round-robin matchday
   * (e.g. knockout stages).
   */
  round?: number | null;

  /**
   * @property {MatchType} matchType - The category or type of the match (e.g., Regular Season, Playoff).
   */
  matchType: MatchType;

  /**
   * @property {string} slug - The unique, URL-friendly identifier used in public match links.
   */
  slug: string;

  /**
   * @property {ITeamMatchResponse} homeTeam - Details of the home team participating in the match.
   */
  homeTeam: ITeamMatchResponse | null;

  /**
   * @property {ITeamMatchResponse} visitorTeam - Details of the visiting team participating in the match.
   */
  visitorTeam: ITeamMatchResponse | null;

  /**
   * @property {boolean} isFinished - A boolean indicating whether the match has concluded.
   */
  isFinished: boolean;

  /**
   * @property {GUID | null} winningTeamId - The unique identifier (GUID) of the team that won the match, or null if the match is not finished or was a draw.
   */
  winningTeamId: GUID | null;

  /**
   * @property {IVenueResponse} venue - Details about the venue where the match was played.
   */
  venue: IVenueResponse | null;

  /**
   * @property {GUID | null} stageId - The unique identifier of the stage to which the match belongs, or null if not assigned.
   */
  stageId: GUID | null;

  /**
   * @property {GUID | null} tournamentId - The tournament this match belongs
   * to (via its stage's division), used to navigate back to the tournament
   * from the public match page. Null for a match with no stage assigned yet.
   */
  tournamentId?: GUID | null;

  /**
   * @property {string | null} winningTeamName - The name of the winning team, or null if the match is not finished or was a draw.
   */
  winningTeamName: string | null;

  /**
   * @property {MatchStatus | null} status - The lifecycle status of the match
   * (Scheduled/Played/Suspended/WalkOver). Lets a walkover be told apart from
   * a normal result. Optional/null when the backend did not populate it.
   */
  status?: MatchStatus | null;
}

/**
 * The minimal response structure for a match, tailored for divisions
 * (e.g. the create-match endpoint and a division's week schedule).
 * Unlike {@link IMatchResponse}, teams are flat name strings rather than
 * nested team objects.
 * @interface IMinimalMatchResponse
 */
export interface IMinimalMatchResponse {
  id: GUID;
  matchDate: string;
  homeTeamName: string;
  visitorTeamName: string;
  homeScore: number | null;
  visitorScore: number | null;
  winningTeamName: string | null;
  isFinished: boolean;
  matchType: MatchType;
  status?: MatchStatus | null;

  /**
   * The matchday (jornada) this match belongs to, 1-based (HU-63/HU-65). Null
   * for matches with no round-robin matchday (e.g. knockout stages).
   * @type {number | null}
   */
  round?: number | null;
}

/**
 * A single matchday (jornada) and the matches played in it (HU-63). Mirrors the
 * backend `RoundMatchesResponse`; returned ordered so the fixture can be
 * rendered grouped by round ("Fecha 1 / Partido 1..2, Fecha 2 / …") instead of
 * by calendar date.
 * @interface IRoundMatchesResponse
 */
export interface IRoundMatchesResponse {
  /**
   * The 1-based round number. Null groups matches with no round-robin matchday
   * (e.g. knockout stages).
   * @type {number | null}
   */
  round: number | null;

  /**
   * The matches played in this round, in a stable order.
   * @type {IMatchResponse[]}
   */
  matches: IMatchResponse[];
}

/**
 * The request body for reprogramming/suspending a match (HU-68). The match is
 * marked suspended and, when a new date is provided, moved to it — never
 * changing its round (HU-67).
 * @interface ISuspendMatchRequest
 */
export interface ISuspendMatchRequest {
  /**
   * Optional new calendar date/time (ISO 8601). When omitted, the match is
   * suspended in place without rescheduling.
   * @type {string}
   */
  matchDate?: string;
}

/**
 * The filter criteria for fetching matches, which includes the home and visitor team names, division name, match type, and finish status.
 * @interface MatchFiltered
 * @extends Filtered
 */
export interface MatchFiltered extends Filtered {
  /**
   * The id of the tournament the match belongs to.
   * @type {GUID}
   */
  tournamentId?: GUID;

  /**
   * The id of the division the match belongs to.
   * @type {GUID}
   */
  divisionId?: GUID;

  /**
   * The name of the home team.
   * @type {string}
   */
  homeTeamName?: string;

  /**
   * The name of the visitor team.
   * @type {string}
   */
  visitorTeamName?: string;

  /**
   * The id of the stage the match belongs to.
   * @type {GUID}
   */
  stageId?: GUID;

  /**
   * The type of match (Regular or Playoff).
   * @type {MatchType}
   */
  type?: MatchType;

  /**
   * Indicates whether the match is finished.
   * @type {boolean}
   */
  isFinished?: boolean;
}

/**
 * The request body structure for updating the score of a match.
 * @interface IPutMatchScoreRequest
 */
export interface IPutMatchScoreRequest {
  /**
   * The new score for the home team.
   * @type {number}
   */
  homeScore: number;

  /**
   * The new score for the visitor team.
   * @type {number}
   */
  visitorScore: number;
}

/**
 * The request body structure for finishing a match by deriving its score
 * from both teams' scoring sheets (HU-72).
 * @interface IPutMatchResultFromSheetsRequest
 */
export interface IPutMatchResultFromSheetsRequest {
  /**
   * The home team's per-player points.
   * @type {PlayerScoreEntry[]}
   */
  homeScores: PlayerScoreEntry[];

  /**
   * The visitor team's per-player points.
   * @type {PlayerScoreEntry[]}
   */
  visitorScores: PlayerScoreEntry[];
}

/**
 * The request body structure for updating the date, venue and teams of a match.
 * @interface IPutMatchRequest
 */
export interface IPutMatchRequest {
  /**
   * The new match date.
   * @type {string}
   */
  matchDate?: string;

  /**
   * The id of the venue where the match will be played.
   * @type {GUID}
   */
  venueId?: GUID;

  /**
   * The ID of the home team.
   * @type {GUID}
   */
  homeTeamId?: GUID;

  /**
   * The ID of the visitor team.
   * @type {GUID}
   */
  visitorTeamId?: GUID;
}

/**
 * @interface IMatchStatusChipProps
 * @description Props for a component that displays the status of a match,
 * such as upcoming, in-progress, or finished.
 */
export interface IMatchStatusChipProps {
  /**
   * @property {string} startTime - The start time of the match, typically in ISO 8601 format.
   */
  startTime: string;

  /**
   * @property {boolean} isFinished - A boolean indicating whether the match has concluded.
   */
  isFinished: boolean;

  /**
   * @property {number} [maxMinutes] - An optional property representing the maximum duration
   * of the match in minutes. Useful for calculating remaining time or progress.
   */
  maxMinutes?: number;
}

export interface IEditMatch extends IPutMatchRequest {
  id: GUID;
  homeScore: number;
  visitorScore: number;
  matchDate: Date;
  isFinished: boolean;
  venue: IVenueResponse | null;

  /**
   * The start date of the stage.
   * @type {string} ISO 8601 format date.
   */
  startDate: string;

  /**
   * The end date of the stage.
   * @type {string} ISO 8601 format date.
   */
  endDate: string;
}

export interface IDashboardMatches {
  matches: IMatchResponse[] | null;
}

/**
 * The request body structure for marking a match as a walkover (HU-73).
 * @interface ILoadWalkOverRequest
 */
export interface ILoadWalkOverRequest {
  /**
   * The team that showed up (the walkover winner). Must be one of the match's
   * two teams.
   * @type {GUID}
   */
  presentTeamId: GUID;

  /**
   * Optional override for the present team's awarded score. When omitted, the
   * backend applies the regulation default.
   * @type {number}
   */
  presentTeamScore?: number;
}
