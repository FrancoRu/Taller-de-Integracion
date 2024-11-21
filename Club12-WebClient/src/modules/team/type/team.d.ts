import { Filtered, GenericResponsePagination } from '../../core/types/types';
import { PlayerResponse } from '../../player/type/player';

/**
 * Context properties and methods for managing teams in a sports system.
 * These methods allow for creating, updating, fetching, and deleting teams.
 * @interface ITeamContextProps
 */
export interface ITeamContextProps {
  /**
   * Adds a new team.
   * @param team The details of the team to add.
   * @returns A promise that resolves with the response containing the newly added team.
   */
  addTeam(team: AddTeamRequest): Promise<TeamResponse | void>;

  /**
   * Adds a batch of teams with associated files (e.g., for a specific division).
   * @param divisionId The ID of the division the teams belong to.
   * @param teamFile The file containing the team data.
   * @param logoFile The file containing the team logo.
   * @returns A promise that resolves with the response containing the added teams.
   */
  addTeamToDivisionIdBatch(
    divisionId: string,
    teamFile: File,
    logoFile: File
  ): Promise<TeamResponse | void>;

  /**
   * Updates an existing team by its ID.
   * @param id The ID of the team to update.
   * @param data The updated team data.
   * @returns A promise that resolves with the updated team details.
   */
  putTeamById(id: string, data: PutTeamRequest): Promise<TeamResponse | void>;

  /**
   * Updates the logo of an existing team.
   * @param id The ID of the team whose logo is to be updated.
   * @param logo The new logo file.
   * @returns A promise that resolves when the logo is successfully updated.
   */
  putTeamLogoById(id: string, logo: File): Promise<void>;

  /**
   * Fetches teams based on filters.
   * @param filters The filters to apply when fetching the teams.
   * @returns A promise that resolves with the paginated response containing teams that match the filters.
   */
  getTeamsByFiltered(
    filters: TeamFiltered
  ): Promise<GenericResponsePagination<TeamResponse> | void>;

  /**
   * Fetches a team by its ID.
   * @param id The ID of the team to fetch.
   * @returns A promise that resolves with the team details.
   */
  getTeamById(id: string): Promise<TeamResponse | void>;

  /**
   * Deletes a team by its ID.
   * @param id The ID of the team to delete.
   * @returns A promise that resolves when the team is successfully deleted.
   */
  deleteTeamById(id: string): Promise<void>;
}

/**
 * The request body structure for adding a new team.
 * @interface AddTeamRequest
 */
export interface AddTeamRequest {
  /**
   * The name of the team.
   * @type {string}
   */
  name: string;

  /**
   * The three-letter code representing the team.
   * @type {string}
   */
  threeLetterCode: string;

  /**
   * The shirt color of the team.
   * @type {string}
   */
  shirtColor: string;

  /**
   * The ID of the division the team belongs to.
   * @type {string}
   */
  divisionId: string;

  /**
   * The logo file of the team.
   * @type {File}
   */
  logo: File;
}

/**
 * The response structure for a team.
 * @interface TeamResponse
 */
export interface TeamResponse {
  /**
   * The unique ID of the team.
   * @type {string}
   */
  id: string;

  /**
   * The name of the team.
   * @type {string}
   */
  name: string;

  /**
   * The three-letter code representing the team.
   * @type {string}
   */
  threeLetterCode: string;

  /**
   * The shirt color of the team.
   * @type {string}
   */
  shirtColor: string;

  /**
   * The ID of the division the team belongs to.
   * @type {string}
   */
  divisionId: string;

  /**
   * The URL of the team's logo.
   * @type {string}
   */
  logoUrl: string;

  /**
   * A list of players on the team.
   * @type {PlayerResponse[]}
   */
  players: PlayerResponse[];
}

/**
 * The request body structure for updating an existing team.
 * @interface PutTeamRequest
 */
export interface PutTeamRequest {
  /**
   * The updated name of the team.
   * @type {string}
   */
  name?: string;

  /**
   * The updated three-letter code representing the team.
   * @type {string}
   */
  threeLetterCode?: string;

  /**
   * The updated shirt color of the team.
   * @type {string}
   */
  shirtColor?: string;
}

/**
 * The filters for fetching teams.
 * @interface TeamFiltered
 */
export interface TeamFiltered extends PutTeamRequest, Filtered {}
