import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import { MutationResult } from '@/modules/core/utils/problemDetails';

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
   * Fetches a player by its ID or its public slug.
   * @param idOrSlug The ID or slug of the player to fetch.
   * @param isAdministrative Whether to use the administrative route to fetch the player. Defaults to false.
   * @returns A promise that resolves with the player details.
   */
  getPlayerById(
    idOrSlug: string,
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
   * Deletes a player by its ID. Resolves with a discriminated result so callers
   * can surface a backend integrity block (a player with statistics/scorers/
   * sanctions is rejected with a 409 and a Spanish message).
   * @param id The ID of the player to delete.
   */
  deletePlayerById(id: GUID): Promise<MutationResult>;

  /**
   * Registers a player onto a team's roster for a tournament season,
   * optionally assigning a dorsal (HU-54). Resolves with a discriminated
   * result so callers can surface the specific roster-invariant conflict
   * (duplicate dorsal / roster full / already in another team) returned as a
   * 409 by the backend.
   * @param playerId The player to register.
   * @param request The team, tournament and optional dorsal.
   */
  registerPlayerToTeam(
    playerId: GUID,
    request: IRegisterPlayerToTeamRequest
  ): Promise<PlayerRegistrationResult>;
}

/**
 * The request body for registering a player onto a team roster (HU-54).
 * @interface IRegisterPlayerToTeamRequest
 */
export interface IRegisterPlayerToTeamRequest {
  teamId: GUID;
  tournamentId: GUID;
  /** The dorsal to assign for this team/season, or null to leave it unset. */
  jerseyNumber?: number | null;
}

/**
 * The successful outcome of a roster registration (HU-54).
 * @interface IPlayerRegistrationResponse
 */
export interface IPlayerRegistrationResponse {
  playerId: GUID;
  teamId: GUID;
  tournamentId: GUID;
  jerseyNumber?: number | null;
}

/**
 * Discriminated result of {@link IPlayerContextProps.registerPlayerToTeam}:
 * either the registration succeeded, or it failed with a user-facing message
 * mapped from the backend roster conflict (HU-54).
 */
export type PlayerRegistrationResult =
  | { success: true; data: IPlayerRegistrationResponse }
  | { success: false; errorMessage: string };

/**
 * The filter criteria for fetching players, which includes the player's name and document number.
 * @interface PlayerFiltered
 */
export interface PlayerFiltered extends Filtered {
  /**
   * The name(s) of the player to filter by.
   * @type {string}
   */
  names?: string;

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

  isFederated?: boolean;

  club?: string;

  category?: string;
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
  secondName?: string;

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

  birthDate: Date;

  phoneNumber: string;

  socialSecurity: string;
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

  /**
   * The unique, URL-friendly identifier used in public player links.
   * @type {string}
   */
  slug: string;

  fullName: string;

  isFederated: boolean;

  club: string;

  category: string;

  /**
   * The player's medical-record status for the season roster this response
   * belongs to (HU-57), when the backend populated it for a specific season.
   * @type {MedicalRecordStatus}
   */
  medicalRecordStatus?: MedicalRecordStatus | null;

  /**
   * Whether the player is habilitado (medical record Approved) for this
   * season roster (HU-57).
   * @type {boolean}
   */
  isHabilitado?: boolean;

  /**
   * The player's dorsal (jersey number) for this season roster (HU-54). Null
   * or undefined when unassigned or when the roster was not loaded for a
   * specific season.
   * @type {number}
   */
  jerseyNumber?: number | null;
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

  /**
   * The player's medical-record status for the season roster this response
   * belongs to (HU-57). Null/undefined when the roster was not loaded for a
   * specific season.
   * @type {MedicalRecordStatus}
   */
  medicalRecordStatus?: MedicalRecordStatus | null;

  /**
   * Whether the player is habilitado (medical record Approved) for this
   * season roster (HU-57), so the UI can flag not-habilitado players (HU-62).
   * @type {boolean}
   */
  isHabilitado?: boolean;

  /**
   * The player's dorsal (jersey number) for this season roster (HU-54). Null
   * or undefined when unassigned.
   * @type {number}
   */
  jerseyNumber?: number | null;
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
  secondName?: string;

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

  teamId?: GUID;

  isFederated?: boolean;

  club?: string;

  category?: string;
}
