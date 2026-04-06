import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import {
  IPublicPlayerResponse,
  IPlayerResponse,
} from '@/modules/player/type/player.d';

/**
 * Context properties and methods for managing teams in a sports system.
 * These methods allow for creating, updating, fetching, and deleting teams.
 * @interface ITeamContextProps
 */
export interface ITeamContextProps {
  team: ITeamResponse | null;
  teams: ITeamResponse[] | null;
  /**
   * Adds a new team.
   * @param team The details of the team to add.
   * @returns A promise that resolves with the response containing the newly added team.
   */
  addTeam(team: IAddTeamRequest): Promise<ITeamResponse | void>;

  /**
   * Updates an existing team by its ID.
   * @param id The ID of the team to update.
   * @param data The updated team data.
   * @returns A promise that resolves with the updated team details.
   */
  putTeamById(id: GUID, data: IPutTeamRequest): Promise<ITeamResponse | void>;

  /**
   * Updates the logo of an existing team.
   * @param id The ID of the team whose logo is to be updated.
   * @param logo The new logo file.
   * @returns A promise that resolves when the logo is successfully updated.
   */
  putTeamLogoById(id: GUID, logo: File): Promise<void>;

  /**
   * Fetches teams based on filters.
   * @param filters The filters to apply when fetching the teams.
   * @returns A promise that resolves with the paginated response containing teams that match the filters.
   */
  getTeamsByFiltered(
    filters: TeamFiltered
  ): Promise<GenericResponsePagination<ITeamResponse> | void>;

  /**
   * Fetches a team by its ID.
   * @param id The ID of the team to fetch.
   * @returns A promise that resolves with the team details.
   */
  getTeamById(id: GUID): Promise<ITeamResponse | void>;

  /**
   * Deletes a team by its ID.
   * @param id The ID of the team to delete.
   * @returns A promise that resolves when the team is successfully deleted.
   */
  deleteTeamById(id: GUID): Promise<void>;
}

/**
 * The request body structure for adding a new team.
 * @interface IAddTeamRequest
 */
export interface IAddTeamRequest {
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
   * The logo file of the team.
   * @type {File}
   */
  logo: File;

  /**
   * The ID of the tournament the team belongs to.
   * @type {GUID | null}
   */
  tournamentId?: GUID | null;
}

/**
 * The response structure for a team.
 * @interface ITeamResponse
 */
export interface ITeamResponse {
  /**
   * The unique ID of the team.
   * @type {GUID}
   */
  id: GUID;

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
   * The URL of the team's logo.
   * @type {string}
   */
  logoUrl: string;

  /**
   * A list of players on the team.
   * @type {IPlayerResponse[]}
   */
  players: IPlayerResponse[];

  tournamentId: GUID | null;
}

/**
 * The request body structure for updating an existing team.
 * @interface IPutTeamRequest
 */
export interface IPutTeamRequest {
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
 * @interface ITeamMatchResponse
 * @description Represents the response structure for a team in a match,
 * including its identification, visual details, and score.
 */
export interface ITeamMatchResponse {
  /**
   * @property {GUID} id - The unique identifier (GUID) for the team.
   */
  id: GUID;

  /**
   * @property {string} name - The name of the team.
   */
  name: string;

  /**
   * @property {string} logoUrl - The URL pointing to the team's logo image.
   */
  logoUrl: string;

  /**
   * @property {number} score - The score achieved by the team in the match.
   */
  score: number;

  players: IPublicPlayerResponse[];
}
/**
 * The filters for fetching teams.
 * @interface TeamFiltered
 */
export interface TeamFiltered extends IPutTeamRequest, Filtered {
  stageId?: GUID;
  tournamentId?: GUID;
}
