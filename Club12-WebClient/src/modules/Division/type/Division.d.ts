import { Filtered, GenericResponsePagination } from "../../core/types/types";
import { MatchResponse } from "../../match/type/match";

/**
 * Context properties and methods for managing divisions in a React application.
 * These methods interact with the backend for creating, updating, fetching, and deleting divisions.
 * @interface IDivisionContextProps
 */
export interface IDivisionContextProps {
  /**
   * Adds a new division to the system.
   * @param division The details of the division to add.
   * @returns A promise that resolves with the response containing the newly added division.
   */
  addDivision(division: AddDivisionRequest): Promise<DivisionResponse | void>;

  /**
   * Generates fixtures for a division based on its ID.
   * @param id The ID of the division for which to generate fixtures.
   * @returns A promise that resolves when the fixtures are successfully generated.
   */
  generateFixtureByDivisionId(id: string): Promise<void>;

  /**
   * Updates an existing division by its ID.
   * @param id The ID of the division to update.
   * @param division The updated division data.
   * @returns A promise that resolves with the response containing the updated division.
   */
  putDivisionById(
    id: string,
    division: PutDivisionRequest
  ): Promise<DivisionResponse | void>;

  /**
   * Fetches a division by its ID.
   * @param id The ID of the division to fetch.
   * @returns A promise that resolves with the division data.
   */
  getDivisionsById(id: string): Promise<DivisionResponse | void>;

  /**
   * Fetches divisions based on filters and pagination.
   * @param filter The filter criteria to apply when fetching divisions.
   * @returns A promise that resolves with a paginated response containing filtered divisions.
   */
  getDivisionsByFilters(
    filter: DivisionFiltered
  ): Promise<GenericResponsePagination<DivisionResponse> | void>;

  /**
   * Fetches the top scores for a division by its ID.
   * @param id The ID of the division to fetch top scores for.
   * @returns A promise that resolves with an array of top score responses for the division.
   */
  getTopScoresByDivisionId(
    id: string
  ): Promise<DivisionTopScoreResponse[] | void>;

  /**
   * Deletes a division by its ID.
   * @param id The ID of the division to delete.
   * @returns A promise that resolves when the division is successfully deleted.
   */
  deleteDivisionsById(id: string): Promise<void>;
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
   * @type {string}
   */
  tournamentId: string;
}

/**
 * The response structure for a division, including details about the division, its matches, and positions.
 * @interface DivisionResponse
 */
export interface DivisionResponse {
  /**
   * The unique identifier of the division.
   * @type {string}
   */
  id: string;

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

  /**
   * The list of positions for teams in the division.
   * @type {Position[]}
   */
  positions: Position[];

  /**
   * The ID of the tournament to which the division belongs.
   * @type {string}
   */
  tournamentId: string;

  /**
   * The list of matches for the division, grouped by week.
   * @type {MatchResponse[]}
   */
  matchesByWeek: MatchResponse[];
}

/**
 * The response structure for the top scores in a division, including the player details and weekly scores.
 * @interface DivisionTopScoreResponse
 */
export interface DivisionTopScoreResponse {
  /**
   * The unique identifier of the player.
   * @type {string}
   */
  playerId: string;

  /**
   * The first name of the player.
   * @type {string}
   */
  firstName: string;

  /**
   * The last name of the player.
   * @type {string}
   */
  lastName: string;

  /**
   * An array of the player's weekly scores.
   * @type {number[]}
   */
  weeklyScores: number[];

  /**
   * The total points accumulated by the player.
   * @type {number}
   */
  totalPoints: number;
}

/**
 * The structure for a position in a division, including team statistics.
 * @type Position
 */
type Position = {
  /**
   * The unique identifier of the team.
   * @type {string}
   */
  teamId: string;

  /**
   * The name of the team.
   * @type {string}
   */
  teamName: string;

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
   * The total points the team has earned.
   * @type {number}
   */
  points: number;
};

/**
 * The filter criteria for fetching divisions, which extends from PutDivisionRequest and Filtered.
 * This includes the `isFinished` property to filter divisions by their completion status.
 * @interface DivisionFiltered
 * @extends PutDivisionRequest
 * @extends Filtered
 */
export interface DivisionFiltered extends PutDivisionRequest, Filtered {
  /**
   * Indicates whether to fetch finished divisions only.
   * @type {boolean}
   */
  isFinished?: boolean;
}

/**
 * The request body structure for updating an existing division.
 * @interface PutDivisionRequest
 */
export interface PutDivisionRequest {
  /**
   * The updated name of the division.
   * @type {string}
   */
  name?: string;
}
