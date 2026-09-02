import { FetchOptions, Filtered, GUID } from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';

/**
 * A tournament that belongs to a season, as embedded in a {@link ISeasonResponse}.
 * A season groups the Masculino and Femenino tournaments played in a given year,
 * so each entry carries the {@link TournamentCategory} used to group them in the
 * public season view.
 * @interface ISeasonTournament
 */
export interface ISeasonTournament {
  /** The unique identifier of the tournament. */
  id: GUID;
  /** The name of the tournament. */
  name: string;
  /** The unique, URL-friendly identifier used in public tournament links. */
  slug: string;
  /** Competitive category (gender) of the tournament (HU-48). */
  category: TournamentCategory;
  /** The tournament's lifecycle status. */
  status: TournamentStatus;
}

/**
 * The response structure for a season ("Temporada"): a named grouping of the
 * tournaments played in a year.
 * @interface ISeasonResponse
 */
export interface ISeasonResponse {
  /** The unique identifier of the season. */
  id: GUID;
  /** The name of the season. */
  name: string;
  /** The unique, URL-friendly identifier used in public season links. */
  slug: string;
  /** The calendar year of the season, when set. */
  year: number | null;
  /** The tournaments grouped under this season. */
  tournaments: ISeasonTournament[];
}

/**
 * The request body structure for adding a new season.
 * @interface IAddSeasonRequest
 */
export interface IAddSeasonRequest {
  /** The name of the season. */
  name: string;
  /** The calendar year of the season (optional). */
  year?: number | null;
}

/**
 * The request body structure for updating an existing season. Every field is
 * optional so a caller can update only what changed.
 * @interface IPutSeasonRequest
 */
export interface IPutSeasonRequest {
  /** The name of the season. */
  name?: string;
  /** The calendar year of the season. */
  year?: number | null;
}

/**
 * The structure for filtering/paginating seasons.
 * @interface SeasonFiltered
 */
export interface SeasonFiltered extends Filtered {
  /** The name of the season. */
  name?: string;
}

/**
 * Context properties and methods for managing season data in a React
 * application. These methods interact with the backend for creating, updating,
 * fetching, and deleting seasons.
 * @interface ISeasonContextProps
 */
export interface ISeasonContextProps {
  season: ISeasonResponse | null;
  seasons: ISeasonResponse[] | null;

  /**
   * Adds a new season to the system.
   * @param season The details of the season to add.
   * @returns A promise that resolves with the response containing the newly added season.
   */
  addSeason(season: IAddSeasonRequest): Promise<ISeasonResponse | void>;

  /**
   * Updates an existing season.
   * @param id The ID of the season to update.
   * @param season The updated season details.
   * @returns A promise that resolves with the response containing the updated season.
   */
  putSeasonById(
    id: GUID,
    season: IPutSeasonRequest
  ): Promise<ISeasonResponse | void>;

  /**
   * Fetches all seasons (a plain array; seasons are few, no pagination).
   * @param filter The filter criteria to apply when fetching seasons.
   * @returns A promise that resolves with the array of seasons.
   */
  getSeasonsByFiltered(
    filter: SeasonFiltered,
    options?: FetchOptions
  ): Promise<ISeasonResponse[] | void>;

  /**
   * Fetches a specific season by its unique ID or its public slug.
   * @param idOrSlug The ID or slug of the season to fetch.
   * @param options Per-call options; `silent` suppresses the global alert on failure.
   * @returns A promise that resolves with the season data.
   */
  getSeasonById(
    idOrSlug: string,
    options?: FetchOptions
  ): Promise<ISeasonResponse | void>;

  /**
   * Deletes a season by its unique ID.
   * @param id The ID of the season to delete.
   * @returns A promise resolving to `true` if the season was deleted, `false`
   * if the request failed (the global error is already reported either way).
   */
  deleteSeasonById(id: GUID): Promise<boolean>;
}
