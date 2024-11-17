import { PlayerResponse } from "../../player/type/player";

/**
 * Context properties and methods for managing player statistics in a sports system.
 * These methods allow for creating, updating, fetching, and deleting player statistics.
 * @interface IPlayerStatisticContextProps
 */
export interface IPlayerStatisticContextProps {
  /**
   * Adds a new player statistic.
   * @param playerStatistic The details of the player statistic to add.
   * @returns A promise that resolves with the response containing the newly added player statistic.
   */
  addPlayerStatistic(
    playerStatistic: AddPlayerStatisticRequest
  ): Promise<PlayerResponse>;

  /**
   * Updates an existing player statistic.
   * @param playerStatistic The updated player statistic details.
   * @returns A promise that resolves when the player statistic is successfully updated.
   */
  putPlayerStatistic(playerStatistic: PutPlayerStatisticRequest): Promise<void>;

  /**
   * Fetches a player statistic by its ID.
   * @param id The ID of the player statistic to fetch.
   * @returns A promise that resolves with the player statistic details.
   */
  getPlayerStatisticById(id: string): Promise<PlayerResponse>;

  /**
   * Deletes a player statistic by its ID.
   * @param id The ID of the player statistic to delete.
   * @returns A promise that resolves when the player statistic is successfully deleted.
   */
  deletePlayerStatisticById(id: string): Promise<void>;
}

/**
 * The request body structure for adding a new player statistic.
 * @interface AddPlayerStatisticRequest
 */
export interface AddPlayerStatisticRequest {
  /**
   * The value of the player statistic (e.g., number of goals, points, etc.).
   * @type {number}
   */
  value: number;

  /**
   * The ID of the match in which the statistic was recorded.
   * @type {string}
   */
  matchId: string;

  /**
   * The ID of the player for whom the statistic is recorded.
   * @type {string}
   */
  playerId: string;
}

/**
 * The response structure for a player statistic.
 * @interface PlayerStatisticResponse
 */
export interface PlayerStatisticResponse {
  /**
   * The unique identifier of the player statistic.
   * @type {string}
   */
  id: string;

  /**
   * The ID of the player for whom the statistic is recorded.
   * @type {string}
   */
  playerId: string;

  /**
   * The value of the player statistic.
   * @type {number}
   */
  value: number;

  /**
   * The ID of the match in which the statistic was recorded.
   * @type {string}
   */
  matchId: string;
}

/**
 * The request body structure for updating a player statistic.
 * @interface PutPlayerStatisticRequest
 */
export interface PutPlayerStatisticRequest {
  /**
   * The updated value of the player statistic.
   * @type {number}
   */
  value: number;
}
