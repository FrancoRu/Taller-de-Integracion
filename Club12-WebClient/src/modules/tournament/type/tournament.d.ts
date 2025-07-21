import { IStageResponse } from '@/modules/stage/type/stage';
import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '../../core/types/types.d';
import { IDivisionResponse } from '../../division/type/division';

/**
 * Context properties and methods for managing tournaments.
 * These methods allow for creating, updating, fetching, and deleting tournaments.
 * @interface ITournamentContextProps
 */
export interface ITournamentContextProps {
  tournament: ITournamentResponse | null;
  tournaments: ITournamentResponse[] | null;

  /**
   * Adds a new tournament.
   * @param tournament The details of the tournament to add.
   * @returns A promise that resolves with the response containing the newly added tournament.
   */
  addTournament(
    tournament: AddTournamentRequest
  ): Promise<ITournamentResponse | void>;

  /**
   * Updates an existing tournament by its ID.
   * @param id The ID of the tournament to update.
   * @param tournament The updated tournament data.
   * @returns A promise that resolves when the tournament is successfully updated.
   */
  putTournamentById(id: GUID, tournament: IPutTournamentRequest): Promise<void>;

  /**
   * Fetches a tournament by its ID.
   * @param id The ID of the tournament to fetch.
   * @returns A promise that resolves with the tournament details.
   */
  getTournamentById(id: GUID): Promise<ITournamentResponse | void>;

  /**
   * Fetches tournaments based on filters.
   * @param filter The filters to apply when fetching tournaments.
   * @returns A promise that resolves with the paginated response containing tournaments that match the filters.
   */
  getAllTournamentsByFilter(
    filter: ITournamentFiltered
  ): Promise<GenericResponsePagination<ITournamentResponse> | void>;

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
export interface ITournamentResponse {
  /**
   * The unique ID of the tournament.
   * @type {GUID}
   */
  id: GUID;

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
   * @type {IDivisionResponse[]}
   */
  divisions?: IDivisionResponse[];
}

/**
 * The structure for filtering tournaments.
 * @interface ITournamentFiltered
 */
export interface ITournamentFiltered extends Filtered {
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

/**
 * The request body structure for updating an existing tournament.
 * @interface PutTournamentRequest
 */
export interface IPutTournamentRequest {
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

export type StatisticsPositions = {
  pj: number;
  pg: number;
  pp: number;
  gf: number;
  gc: number;
  dif: number;
  pts: number;
};

export type DataPositions = {
  id: string;
  nameTeam: string;
  positions: StatisticsPositions;
};
