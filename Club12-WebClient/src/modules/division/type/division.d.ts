import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';

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
    filter: DivisionFiltered
  ): Promise<GenericResponsePagination<IDivisionResponse> | void>;

  /**
   * Deletes a division by its ID.
   * @param id The ID of the division to delete.
   * @returns A promise that resolves when the division is successfully deleted.
   */
  deleteDivisionsById(id: GUID): Promise<void>;
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
   * The list of positions for teams in the division.
   * @type {Position[]}
   */
  positions?: Position[];

  /**
   * The ID of the tournament to which the division belongs.
   * @type {GUID}
   */
  tournamentId: GUID;

  /**
   * Whether this division is a cross-division cup (exempt from the "one
   * team, one division" rule).
   * @type {boolean}
   */
  isCrossDivisionCup: boolean;
}

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
   * The total points the team has earned.
   * @type {number}
   */
  points: number;
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
