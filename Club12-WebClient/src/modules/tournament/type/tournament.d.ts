import { Filtered, GenericResponsePagination } from "../../core/types/types";
import { DivisionResponse } from "../../division/type/division";

/**
 * Context properties and methods for managing tournaments.
 * These methods allow for creating, updating, fetching, and deleting tournaments.
 * @interface ITournamentContextProps
 */
export interface ITournamentContextProps {
  /**
   * Adds a new tournament.
   * @param tournament The details of the tournament to add.
   * @returns A promise that resolves with the response containing the newly added tournament.
   */
  addTournament(tournament: AddTournamentRequest): Promise<TournamentResponse>;

  /**
   * Updates an existing tournament by its ID.
   * @param id The ID of the tournament to update.
   * @param tournament The updated tournament data.
   * @returns A promise that resolves when the tournament is successfully updated.
   */
  putTournamentById(
    id: string,
    tournament: PutTournamentRequest
  ): Promise<void>;

  /**
   * Fetches a tournament by its ID.
   * @param id The ID of the tournament to fetch.
   * @returns A promise that resolves with the tournament details.
   */
  getTournamentById(id: string): Promise<TournamentResponse>;

  /**
   * Fetches tournaments based on filters.
   * @param filter The filters to apply when fetching tournaments.
   * @returns A promise that resolves with the paginated response containing tournaments that match the filters.
   */
  getAllTournamentsByFilter(
    filter: TournamentFiltered
  ): Promise<GenericResponsePagination<TournamentResponse>>;

  /**
   * Deletes a tournament by its ID.
   * @param id The ID of the tournament to delete.
   * @returns A promise that resolves when the tournament is successfully deleted.
   */
  deleteTournamentById(id: string): Promise<void>;
}

/**
 * The request body structure for adding a new tournament.
 * @interface AddTournamentRequest
 */
export interface AddTournamentRequest {
  /**
   * The name of the tournament.
   * @type {string}
   */
  name: string;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description: string;
}

/**
 * The response structure for a tournament.
 * @interface TournamentResponse
 */
export interface TournamentResponse {
  /**
   * The unique ID of the tournament.
   * @type {string}
   */
  id: string;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description: string;

  /**
   * The name of the tournament.
   * @type {string}
   */
  name: string;

  /**
   * The division associated with the tournament.
   * @type {DivisionResponse}
   */
  division: DivisionResponse;
}

/**
 * The structure for filtering tournaments.
 * @interface TournamentFiltered
 */
export interface TournamentFiltered extends PutTournamentRequest, Filtered {}

/**
 * The request body structure for updating an existing tournament.
 * @interface PutTournamentRequest
 */
export interface PutTournamentRequest {
  /**
   * The name of the tournament.
   * @type {string}
   */
  name?: string;

  /**
   * A description of the tournament.
   * @type {string}
   */
  description?: string;
}
