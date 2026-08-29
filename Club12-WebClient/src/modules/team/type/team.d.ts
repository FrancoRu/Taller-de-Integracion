import {
  FetchOptions,
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { IPublicPlayerResponse } from '@/modules/player/type/player.d';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';

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
   * @returns A promise that resolves to whether the update succeeded (a PUT
   * answers 204 with no body, so there is no updated entity to return).
   */
  putTeamById(id: GUID, data: IPutTeamRequest): Promise<boolean>;

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
    filters: TeamFiltered,
    options?: FetchOptions
  ): Promise<GenericResponsePagination<ITeamResponse> | void>;

  /**
   * Fetches a team by its ID or its public slug.
   * @param idOrSlug The ID or slug of the team to fetch.
   * @param options Per-call options; `silent` suppresses the global alert on failure.
   * @returns A promise that resolves with the team details.
   */
  getTeamById(
    idOrSlug: string,
    options?: FetchOptions
  ): Promise<ITeamResponse | void>;

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
   * The secondary shirt color, used for the jersey pattern and trim.
   * @type {string | null}
   */
  shirtSecondaryColor?: string | null;

  /**
   * The selected jersey kit template (e.g. `solid`, `stripes`).
   * @type {string | null}
   */
  jerseyStyle?: string | null;

  /**
   * The logo file of the team.
   * @type {File}
   */
  logo: File;

  /**
   * The ID of the tournament the team belongs to.
   * @type {GUID}
   */
  tournamentId?: GUID;
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
   * The unique, URL-friendly identifier used in public team links.
   * @type {string}
   */
  slug: string;

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
   * The secondary shirt color, used for the jersey pattern and trim.
   * @type {string | null}
   */
  shirtSecondaryColor?: string | null;

  /**
   * The selected jersey kit template (e.g. `solid`, `stripes`).
   * @type {string | null}
   */
  jerseyStyle?: string | null;

  /**
   * The URL of the team's logo.
   * @type {string}
   */
  logoUrl: string;

  /**
   * A list of players on the team.
   * @type {IPublicPlayerResponse[]}
   */
  players: IPublicPlayerResponse[];

  tournamentId: GUID | null;

  /**
   * The stable cross-season club this team belongs to (HU-99), when linked.
   * Absent/undefined until the team is associated with a club.
   * @type {GUID | null}
   */
  clubId?: GUID | null;
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

  /**
   * The updated secondary shirt color, used for the jersey pattern and trim.
   * @type {string | null}
   */
  shirtSecondaryColor?: string | null;

  /**
   * The updated jersey kit template (e.g. `solid`, `stripes`).
   * @type {string | null}
   */
  jerseyStyle?: string | null;
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

  /** Primary shirt color (#rrggbb), for rendering the kit on the scoreboard. */
  shirtColor?: string | null;

  /** Jersey kit pattern (e.g. "solid", "stripes"). */
  jerseyStyle?: string | null;

  /** Secondary shirt color (#rrggbb), for the kit trim/pattern. */
  shirtSecondaryColor?: string | null;

  /**
   * @property {number} score - The score achieved by the team in the match.
   */
  score: number;

  players: IPublicPlayerResponse[];

  /**
   * @property {IScorerByPlayerResponse[]} scorers - The scorers for the team in the match.
   */
  scorers: IScorerByPlayerResponse[];
}
/**
 * The filters for fetching teams.
 * @interface TeamFiltered
 */
export interface TeamFiltered extends IPutTeamRequest, Filtered {
  stageId?: GUID;
  tournamentId?: GUID;
}
