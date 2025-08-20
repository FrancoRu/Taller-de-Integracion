import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '../../core/types/types';

/**
 * Context properties and methods for managing players in a sports system.
 * These methods interact with the backend for creating, updating, fetching, and deleting players.
 * @interface IPlayerContextProps
 */
export interface IPlayerContextProps {
  player: IPlayerResponse | null;
  players: IPlayerResponse[] | null;
  /**
   * Adds a new player to the system.
   * @param player The details of the player to add.
   * @returns A promise that resolves with the response containing the newly added player.
   */
  addPlayer(player: IAddPlayerRequest): Promise<IPlayerResponse | void>;

  /**
   * Fetches a player by its ID.
   * @param id The ID of the player to fetch.
   * @param isAdministrative Whether to use the administrative route to fetch the player. Defaults to false.
   * @returns A promise that resolves with the player details.
   */
  getPlayerById(
    id: GUID,
    isAdministrative: boolean = false
  ): Promise<IPlayerResponse | void>;

  /**
   * Fetches players based on filters and pagination.
   * @param filter The filter criteria to apply when fetching players.
   * @returns A promise that resolves with a paginated response containing filtered players.
   */
  getPlayersByFilter(
    filter: PlayerFiltered
  ): Promise<GenericResponsePagination<IPlayerResponse> | void>;

  /**
   * Updates a player's information.
   * @param id The ID of the player to update.
   * @param player The updated player details.
   * @returns A promise that resolves when the player is successfully updated.
   */
  putPlayerById(
    id: GUID,
    player: IPutPlayerRequest
  ): Promise<IPlayerResponse | void>;

  /**
   * Deletes a player by its ID.
   * @param id The ID of the player to delete.
   * @returns A promise that resolves when the player is successfully deleted.
   */
  deletePlayerById(id: GUID): Promise<void>;
}

/**
 * The filter criteria for fetching players, which includes the player's name and document number.
 * @interface PlayerFiltered
 */
export interface PlayerFiltered extends Filtered {
  /**
   * The first name of the player.
   * @type {string}
   */
  firstName?: string;

  /**
   * The second name of the player (if applicable).
   * @type {string}
   */
  secondName?: string;

  /**
   * The last name of the player.
   * @type {string}
   */
  lastName?: string;

  /**
   * The document number of the player (e.g., ID, passport).
   * @type {string}
   */
  documentNumber?: string;

  /**
   * The ID of the team the player belongs to.
   * @type {GUID}
   */
  teamId?: GUID;

  birthDate?: Date;

  phoneNumber?: string;

  socialSecurity?: string;
}

/**
 * The request body structure for adding a new player.
 * @interface IAddPlayerRequest
 */
export interface IAddPlayerRequest {
  /**
   * The first name of the player.
   * @type {string}
   */
  firstName: string;

  /**
   * The second name of the player (if applicable).
   * @type {string}
   */
  secondName: string;

  /**
   * The last name of the player.
   * @type {string}
   */
  lastName: string;

  /**
   * The document number of the player (e.g., ID, passport).
   * @type {string}
   */
  documentNumber: string;

  /**
   * The ID of the team the player belongs to.
   * @type {GUID}
   */
  teamId: GUID;

  birthDate?: Date;

  phoneNumber?: string;

  socialSecurity?: string;
}

/**
 * The response structure for a player, including the player's personal information and team ID.
 * @interface PlayerResponse
 * @extends IAddPlayerRequest
 */
export interface IPlayerResponse extends IAddPlayerRequest {
  /**
   * The unique identifier of the player.
   * @type {string}
   */
  id: GUID;

  fullName: string;
}

export interface IPublicPlayerResponse {
  /**
   * The unique identifier of the player.
   * @type {GUID}
   */
  id: GUID;

  /**
   * The first name of the player.
   * @type {string}
   */
  firstName: string;

  /**
   * The second name of the player.
   * @type {string}
   */
  secondName: string;

  /**
   * The last name of the player.
   * @type {string}
   */
  lastName: string;

  /**
   * The full name of the player.
   * @type {string}
   */
  fullName: string;

  /**
   * The unique identifier of the team to which the player belongs.
   * @type {GUID}
   */
  teamId: GUID;
}

/**
 * The request body structure for updating a player's information.
 * @interface IPutPlayerRequest
 */
export interface IPutPlayerRequest {
  /**
   * The first name of the player.
   * @type {string}
   */
  firstName: string;

  /**
   * The second name of the player (if applicable).
   * @type {string}
   */
  secondName: string;

  /**
   * The last name of the player.
   * @type {string}
   */
  lastName?: string;

  /**
   * The document number of the player.
   * @type {string}
   */
  documentNumber?: string;

  birthDate?: Date;

  phoneNumber?: string;

  socialSecurity?: string;

  teamId: GUID;
}
