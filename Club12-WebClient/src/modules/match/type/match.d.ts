import { Filtered, GenericResponsePagination } from '../../core/types/types';
import { VenueResponse } from '../../venue/type/venue';

/**
 * Context properties and methods for managing matches in a sports system.
 * These methods interact with the backend for creating, updating, fetching, and deleting matches.
 * @interface IMatchContextProps
 */
export interface IMatchContextProps {
  /**
   * Adds a new match to the system.
   * @param match The details of the match to add.
   * @returns A promise that resolves with the response containing the newly added match.
   */
  addMatch(match: AddMatchRequest): Promise<MatchResponse | void>;

  /**
   * Updates the score of an existing match.
   * @param id The ID of the match .
   * @param matchScore The new scores for the home and visitor teams.
   * @returns A promise that resolves when the score is successfully updated.
   */
  putMatchScoreByMatchId(
    id: string,
    matchScore: PutMatchScoreRequest
  ): Promise<void>;

  /**
   * Updates the match date and venue.
   * @param id The ID of the match .
   * @param matchDate The new match date.
   * @returns A promise that resolves when the match date and venue are successfully updated.
   */
  putMatchDateByMatchId(
    id: string,
    matchDate: PutMatchDateRequest
  ): Promise<void>;

  /**
   * Fetches a match by its ID.
   * @param id The ID of the match to fetch.
   * @returns A promise that resolves with the match details.
   */
  getMatchById(id: string): Promise<MatchResponse | void>;

  /**
   * Fetches matches based on filters and pagination.
   * @param filter The filter criteria to apply when fetching matches.
   * @returns A promise that resolves with a paginated response containing filtered matches.
   */
  getMatchByFilter(
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<MatchResponse> | void>;

  /**
   * Deletes a match by its ID.
   * @param id The ID of the match to delete.
   * @returns A promise that resolves when the match is successfully deleted.
   */
  deleteMatchById(id: string): Promise<void>;
}

/**
 * The request body structure for adding a new match.
 * @interface AddMatchRequest
 */
export interface AddMatchRequest {
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
   * The match week number.
   * @type {number}
   */
  matchWeek: number;

  /**
   * The ID of the home team.
   * @type {string}
   */
  homeTeamId: string;

  /**
   * The ID of the visitor team.
   * @type {string}
   */
  visitorTeamId: string;

  /**
   * The ID of the division the match belongs to.
   * @type {string}
   */
  divisionId: string;

  /**
   * The ID of the venue where the match will take place.
   * @type {string}
   */
  venueId: string;
}

/**
 * The response structure for a match, including team details, scores, and match results.
 * @interface MatchResponse
 */
export interface MatchResponse {
  /**
   * The unique identifier of the match.
   * @type {string}
   */
  id: string;

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
   * The match week number.
   * @type {number}
   */
  matchWeek: number;

  /**
   * The ID of the home team.
   * @type {string}
   */
  homeTeamId: string;

  /**
   * The name of the home team.
   * @type {string}
   */
  homeTeamName: string;

  /**
   * The ID of the visitor team.
   * @type {string}
   */
  visitorTeamId: string;

  /**
   * The name of the visitor team.
   * @type {string}
   */
  visitorTeamName: string;

  /**
   * The score of the home team.
   * @type {number}
   */
  homeScore: number;

  /**
   * The score of the visitor team.
   * @type {number}
   */
  visitorScore: number;

  /**
   * Indicates whether the match has finished.
   * @type {boolean}
   */
  isFinished: boolean;

  /**
   * The name of the winning team.
   * @type {string}
   */
  winningTeamName: string;

  /**
   * The venue where the match was played.
   * @type {VenueResponse}
   */
  venue: VenueResponse;
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
   * The name of the division the match belongs to.
   * @type {string}
   */
  divisionName?: string;

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
