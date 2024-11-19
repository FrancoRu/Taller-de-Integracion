import { Filetered, GenericResponsePagination } from "../../core/types/types";

/**
 * Context properties and methods for managing players in a sports system.
 * These methods interact with the backend for creating, updating, fetching, and deleting players.
 * @interface IPlayerContextProps
 */
export interface IPlayerContextProps {
  /**
   * Adds a new player to the system.
   * @param player The details of the player to add.
   * @returns A promise that resolves with the response containing the newly added player.
   */
  addPlayer(player: AddPlayerRequest): Promise<PlayerResponse | void>;

  /**
   * Fetches a player by its ID.
   * @param id The ID of the player to fetch.
   * @returns A promise that resolves with the player details.
   */
  getPlayerById(id: string): Promise<PlayerResponse | void>;

  /**
   * Fetches players based on filters and pagination.
   * @param filter The filter criteria to apply when fetching players.
   * @returns A promise that resolves with a paginated response containing filtered players.
   */
  getPlayersByFilter(
    filter: PlayerFiltered
  ): Promise<GenericResponsePagination<PlayerResponse> | void>;

  /**
   * Updates a player's information.
   * @param id The ID of the player to update.
   * @param player The updated player details.
   * @returns A promise that resolves when the player is successfully updated.
   */
  putPlayerById(id: string, player: PutPlayerRequest): Promise<void>;

  /**
   * Deletes a player by its ID.
   * @param id The ID of the player to delete.
   * @returns A promise that resolves when the player is successfully deleted.
   */
  deletePlayerById(id: string): Promise<void>;
}

/**
 * The filter criteria for fetching players, which includes the player's name and document number.
 * @interface PlayerFiltered
 * @extends PutPlayerRequest
 */
export interface PlayerFiltered extends PutPlayerRequest, Filetered {}

/**
 * The request body structure for adding a new player.
 * @interface AddPlayerRequest
 */
export interface AddPlayerRequest {
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
   * @type {string}
   */
  teamId: string;
}

/**
 * The response structure for a player, including the player's personal information and team ID.
 * @interface PlayerResponse
 * @extends AddPlayerRequest
 */
export interface PlayerResponse extends AddPlayerRequest {
  /**
   * The unique identifier of the player.
   * @type {string}
   */
  id: string;
}

/**
 * The request body structure for updating a player's information.
 * @interface PutPlayerRequest
 */
export interface PutPlayerRequest {
  /**
   * The name of the player.
   * @type {string}
   */
  name?: string;

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
}
