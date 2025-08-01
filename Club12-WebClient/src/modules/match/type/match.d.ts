import { GUID } from '@/modules/core/types/types';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IVenueResponse } from '@/modules/venue/type/venue';

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
  addMatch(match: IAddMatchRequest): Promise<IMatchResponse | void>;

  /**
   * Updates the score of an existing match.
   * @param id The ID of the match .
   * @param matchScore The new scores for the home and visitor teams.
   * @returns A promise that resolves when the score is successfully updated.
   */
  putMatchScoreByMatchId(
    id: GUID,
    matchScore: PutMatchScoreRequest
  ): Promise<void>;

  /**
   * Updates the match date and venue.
   * @param id The ID of the match .
   * @param matchDate The new match date.
   * @returns A promise that resolves when the match date and venue are successfully updated.
   */
  putMatchDateByMatchId(
    id: GUID,
    matchDate: PutMatchDateRequest
  ): Promise<void>;

  /**
   * Fetches a match by its ID.
   * @param id The ID of the match to fetch.
   * @returns A promise that resolves with the match details.
   */
  getMatchById(id: GUID): Promise<IMatchResponse | void>;

  /**
   * Fetches matches based on filters and pagination.
   * @param filter The filter criteria to apply when fetching matches.
   * @returns A promise that resolves with a paginated response containing filtered matches.
   */
  getMatchByFilter(
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<IMatchResponse> | void>;

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
   * @type {TypeMatch}
   */
  type: TypeMatch;

  /**
   * The ID of the home team.
   * @type {string}
   */
  homeTeamid: GUID;

  /**
   * The ID of the visitor team.
   * @type {string}
   */
  visitorTeamid: GUID;

  /**
   * The ID of the division the match belongs to.
   * @type {GUID}
   */
  stageId: GUID;

  /**
   * The ID of the venue where the match will take place.
   * @type {GUID}
   */
  venueid: GUID;
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
   * @property {TypeMatch} matchType - The category or type of the match (e.g., Regular Season, Playoff).
   */
  matchType: TypeMatch;

  /**
   * @property {ITeamMatchResponse} homeTeam - Details of the home team participating in the match.
   */
  homeTeam: ITeamMatchResponse;

  /**
   * @property {ITeamMatchResponse} visitorTeam - Details of the visiting team participating in the match.
   */
  visitorTeam: ITeamMatchResponse;

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
  venue: IVenueResponse;
}

/**
 * The types of matches that can exist (Regular or Playoff).
 * @enum TypeMatch
 */
export enum TypeMatch {
  /**
   * A regular match in the tournament.
   * @type {string}
   */
  Regular = 'Regular',

  /**
   * A playoff match in the tournament.
   * @type {string}
   */
  Playoff = 'Playoff',
}

/**
 * The filter criteria for fetching matches, which includes the home and visitor team names, division name, match type, and finish status.
 * @interface MatchFiltered
 * @extends Filtered
 */
export interface MatchFiltered extends Filtered {
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
   * @type {TypeMatch}
   */
  type?: TypeMatch;

  /**
   * Indicates whether the match is finished.
   * @type {boolean}
   */
  isFinished?: boolean;
}

/**
 * The request body structure for updating the score of a match.
 * @interface PutMatchScoreRequest
 */
export interface PutMatchScoreRequest {
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
 * The request body structure for updating the date and venue of a match.
 * @interface PutMatchDateRequest
 */
export interface PutMatchDateRequest {
  /**
   * The new match date.
   * @type {string}
   */
  matchDate?: string;

  /**
   * The ID of the venue where the match will take place.
   * @type {string}
   */
  venueId?: string;
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
