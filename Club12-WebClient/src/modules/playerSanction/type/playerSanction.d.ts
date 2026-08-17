import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';

/**
 * Context properties and methods for managing player sanctions in a sports system.
 * These methods interact with the backend for creating, updating, fetching, and deleting sanctions.
 * @interface IPlayerSanctionContextProps
 */
export interface IPlayerSanctionContextProps {
  /**
   * The current sanction applied to the player.
   * @type {IPlayerSanctionResponse | null}
   */
  playerSanction: IPlayerSanctionResponse | null;

  /**
   * The list of sanctions applied to the match.
   * @type {IPlayerSanctionResponse[] | null}
   */
  playerSanctions: IPlayerSanctionResponse[] | null;

  /**
   * Creates a new player sanction in the system.
   * @param playerSanction The details of the sanction to be created.
   * @returns A promise that resolves with the response containing the newly created sanction, or void if creation fails.
   */
  addPlayerSanction(
    playerSanction: IAddPlayerSanction
  ): Promise<IPlayerSanctionResponse | void>;

  /**
   * Fetches a player sanction by its unique identifier or its slug.
   * @param {string} idOrSlug - The id or slug of the player sanction to retrieve.
   * @returns {Promise<IPlayerSanctionResponse | void>} A promise that resolves with the sanction response or void if not found.
   */
  getPlayerSanctionById(idOrSlug: string): Promise<IPlayerSanctionResponse | void>;

  /**
   * Fetches player sanctions based on filter criteria and pagination.
   * @param filter The filter criteria to apply when fetching sanctions.
   * @returns A promise that resolves with a paginated response containing filtered sanctions, or void if no results are found.
   */
  getPlayerSanctionByFilter(
    filter: IPlayerSanctionFiltered
  ): Promise<GenericResponsePagination<IPlayerSanctionResponse> | void>;

  /**
   * Updates a player's sanction by its ID.
   * @param id The unique identifier (GUID) of the sanction to update.
   * @param playerSanction The updated sanction details.
   * @returns A promise that resolves with the updated sanction or void if the update fails.
   */
  putPlayerSanctionById(
    id: GUID,
    playerSanction: IPutPlayerSanction
  ): Promise<IPlayerSanctionResponse | void>;

  /**
   * Deletes a player's sanction by its ID.
   * @param id The unique identifier (GUID) of the sanction to delete.
   * @returns A promise that resolves when the sanction is successfully deleted.
   */
  deletePlayerSanction(id: GUID): Promise<void>;

  /**
   * Submits an appeal against a sanction.
   */
  appealPlayerSanction(
    id: GUID,
    appeal: IAppealPlayerSanction
  ): Promise<IPlayerSanctionResponse | void>;

  /**
   * Resolves a pending appeal, recording the decision.
   */
  resolvePlayerSanctionAppeal(
    id: GUID,
    resolution: IResolveAppeal
  ): Promise<IPlayerSanctionResponse | void>;
}

/**
 * The response structure for a player sanction, including its details and associated player.
 * @interface IPlayerSanctionResponse
 */
export interface IPlayerSanctionResponse {
  /**
   * The unique identifier of the sanction.
   * @type {GUID}
   */
  id: GUID;

  /**
   * The duration of the sanction (unit depends on business rules, e.g., games, minutes).
   * @type {number}
   */
  duration: number;

  /**
   * The date and time when the sanction was issued.
   * @type {Date}
   */
  issuedDate: Date;

  /**
   * The description or reason for the sanction.
   * @type {string}
   */
  description: string;

  /**
   * The unique, URL-friendly identifier used in sanction links.
   * @type {string}
   */
  slug: string;

  /**
   * The unique identifier of the player who received the sanction.
   * @type {GUID}
   */
  playerId: GUID;

  playerFullName: string;

  matchId: GUID;

  appealStatus: SanctionAppealStatus;

  appealReason?: string | null;

  appealDate?: string | null;

  appealResolution?: string | null;

  appealResolvedDate?: string | null;
}

/**
 * The appeal state of a player sanction.
 */
export type SanctionAppealStatus =
  | 'None'
  | 'Pending'
  | 'Accepted'
  | 'Rejected';

/**
 * Request body for submitting an appeal against a sanction.
 */
export interface IAppealPlayerSanction {
  reason: string;
}

/**
 * Request body for resolving a sanction appeal.
 */
export interface IResolveAppeal {
  accepted: boolean;
  resolution: string;
}

/**
 * The filter criteria for fetching player sanctions, including optional match and player information.
 * @interface IPlayerSanctionFiltered
 * @extends Filtered
 */
export interface IPlayerSanctionFiltered extends Filtered {
  /**
   * The unique identifier of the tournament (optional).
   * @type {GUID}
   */
  tournamentId?: GUID;

  /**
   * The unique identifier of the division (optional).
   * @type {GUID}
   */
  divisionId?: GUID;

  /**
   * The unique identifier of the stage (optional).
   * @type {GUID}
   */
  stageId?: GUID;

  /**
   * The unique identifier of the team (optional).
   * @type {GUID}
   */
  teamId?: GUID;

  /**
   * The unique identifier of the related match (optional).
   * @type {GUID}
   */
  matchId?: GUID;

  /**
   * The unique identifier of the player (optional).
   * @type {GUID}
   */
  playerId?: GUID;

  /**
   * The duration of the sanction (optional).
   * @type {number}
   */
  duration?: number;

  /**
   * The date and time when the sanction was issued (optional).
   * @type {Date}
   */
  issuedDate?: Date;

  /**
   * The description or reason for the sanction (optional).
   * @type {string}
   */
  description?: string;
}

/**
 * The request body structure for adding a new player sanction.
 * @interface IAddPlayerSanction
 */
export interface IAddPlayerSanction {
  /**
   * The duration of the sanction (unit depends on business rules, e.g., games, minutes).
   * @type {number}
   */
  duration: number;

  /**
   * The date and time when the sanction was issued.
   * @type {Date}
   */
  issuedDate: Date;

  /**
   * The description or reason for the sanction.
   * @type {string}
   */
  description: string;

  /**
   * The unique identifier of the related match.
   * @type {GUID}
   */
  matchId: GUID;

  /**
   * The unique identifier of the player who will receive the sanction.
   * @type {GUID}
   */
  playerId: GUID;
}

/**
 * The request body structure for updating an existing player sanction.
 * @interface IPutPlayerSanction
 */
export interface IPutPlayerSanction {
  /**
   * The updated duration of the sanction (unit depends on business rules, e.g., games, minutes).
   * @type {number}
   */
  duration?: number;

  /**
   * The updated description or reason for the sanction.
   * @type {string}
   */
  description?: string;
}

/**
 * Props para el componente InfoPlayerSanctions.
 */
export interface InfoPlayerSanctionsProps {
  /**
   * The unique identifier of the sanction.
   * @type {GUID}
   */
  id: GUID;

  /**
   * Indicates whether the sanction is associated with a player.
   * @type {boolean}
   */
  useWithPlayer: boolean;

  /**
   * The name of the sanction (optional).
   * @type {string | undefined}
   */
  name?: string;

  homeTeamId?: GUID;

  visitorTeamId?: GUID;
}

export interface CreatePlayerFromMatchPlayerSanctionsProps {
  homeTeamId: GUID;

  visitorTeamId: GUID;
}

export type PlayerSanctionsSearchFilters = Pick<
  IPlayerSanctionFiltered,
  | 'tournamentId'
  | 'divisionId'
  | 'stageId'
  | 'matchId'
  | 'teamId'
  | 'playerId'
  | 'description'
>;

export interface IPlayerSanctionCreatePageProps {
  open: boolean;
  onClose: () => void;
  onCreated?: () => void;
}

export interface IPlayerSanctionCreateFormState {
  duration: string;
  issuedDate: string;
  description: string;
  tournamentId: GUID | '';
  divisionId: GUID | '';
  stageId: GUID | '';
  matchId: GUID | '';
  teamId: GUID | '';
  playerId: GUID | '';
}

export interface IPlayerSanctionEditFormState {
  duration: string;
  description: string;
}

export interface IPlayerSanctionDeletePageProps {
  open: boolean;
  sanction: IPlayerSanctionResponse | null;
  onClose: () => void;
  onDeleted?: () => void;
}
