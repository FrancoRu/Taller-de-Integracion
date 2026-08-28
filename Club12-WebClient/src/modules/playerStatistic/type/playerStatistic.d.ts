import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';

/**
 * Context properties and methods for managing player statistics in a sports system.
 * These methods allow for creating, updating, fetching, and deleting player statistics.
 * @interface IPlayerStatisticContextProps
 */
export interface IPlayerStatisticContextProps {
  playerStatistic: PlayerStatisticResponse | null;
  playerStatistics: PlayerStatisticResponse[] | null;
  playerCard: PlayerStatisticCardResponse | null;
  playerHistory: PlayerHistoryResponse | null;

  /**
   * Fetches a player's statistic card (HU-87): total/average points and games
   * played, per season and overall.
   * @param playerId The player's stable id.
   */
  getPlayerCard(
    playerId: GUID
  ): Promise<PlayerStatisticCardResponse | void>;

  /**
   * Fetches a player's cross-season history (HU-88): per season, the team,
   * their stats and their sanctions.
   * @param playerId The player's stable id.
   */
  getPlayerHistory(playerId: GUID): Promise<PlayerHistoryResponse | void>;

  /**
   * Adds a new player statistic.
   * @param playerStatistic The details of the player statistic to add.
   * @returns A promise that resolves with the response containing the newly added player statistic.
   */
  addPlayerStatistic(
    playerStatistic: AddPlayerStatisticRequest
  ): Promise<PlayerStatisticResponse | void>;

  /**
   * Updates an existing player statistic.
   * @param {string} statisticId - The unique identifier of the player statistic to update.
   * @param playerStatistic The updated player statistic details.
   * @returns A promise that resolves when the player statistic is successfully updated.
   */
  putPlayerStatisticById(
    statisticid: GUID,
    playerStatistic: PutPlayerStatisticRequest
  ): Promise<void>;

  /**
   * Fetches a player statistic by its ID.
   * @param id The ID of the player statistic to fetch.
   * @returns A promise that resolves with the player statistic details.
   */
  getPlayerStatisticById(id: GUID): Promise<PlayerStatisticResponse | void>;

  /**
   * Fetches player statistics based on filters and pagination.
   * @param filter The filters to apply when fetching player statistics.
   * @returns A promise that resolves with the paginated statistics list.
   */
  getPlayerStatisticsByFilter(
    filter: PlayerStatisticFiltered
  ): Promise<GenericResponsePagination<PlayerStatisticResponse> | void>;

  /**
   * Deletes a player statistic by its ID.
   * @param id The ID of the player statistic to delete.
   * @returns A promise that resolves when the player statistic is successfully deleted.
   */
  deletePlayerStatisticById(id: GUID): Promise<void>;

  /**
   * Loads a whole team's scoring sheet (planilla) for a match in one coherent
   * operation (HU-71). The listed players' points must add up to the team's
   * final score; otherwise the backend saves nothing and returns 409.
   * @param request The match, team, and per-player points.
   * @returns A promise that resolves with the persisted Points statistics, or
   * void on error (the error message is surfaced globally).
   */
  loadMatchSheet(
    request: LoadMatchSheetRequest
  ): Promise<PlayerStatisticResponse[] | void>;
}

export type StatisticType = 'Points' | 'Assists';

/**
 * One season's scoring line inside a player's statistic card (HU-87). A
 * "season" is the calendar year of the tournament's start date (HU-85).
 */
export interface SeasonStatLine {
  season: number;
  totalPoints: number;
  gamesPlayed: number;
  /** Points per game played that season, rounded to two decimals. */
  averagePoints: number;
}

/**
 * A player's individual statistic card (HU-87): total and average points and
 * games played, both overall and broken down per season (most recent first).
 */
export interface PlayerStatisticCardResponse {
  playerId: GUID;
  fullName: string;
  totalPoints: number;
  gamesPlayed: number;
  /** Overall points per game played, rounded to two decimals. */
  averagePoints: number;
  seasons: SeasonStatLine[];
}

/**
 * A single sanction the player received during a given season (HU-88).
 */
export interface PlayerHistorySanction {
  sanctionId: GUID;
  description: string;
  /** Length in fechas (matchdays), per HU-75. */
  duration: number;
  issuedDate: string;
  matchId: GUID;
}

/**
 * One row of a player's trajectory (HU-88): for a given season/tournament, the
 * team they were registered to, their scoring stats there, and the sanctions
 * they received.
 */
export interface PlayerHistorySeason {
  season: number;
  tournamentId: GUID;
  tournamentName: string;
  teamId: GUID;
  teamName: string;
  totalPoints: number;
  gamesPlayed: number;
  sanctions: PlayerHistorySanction[];
}

/**
 * A player's full cross-season trajectory (HU-88): one entry per season the
 * player was registered, most recent season first.
 */
export interface PlayerHistoryResponse {
  playerId: GUID;
  fullName: string;
  seasons: PlayerHistorySeason[];
}

export interface PlayerStatisticFiltered extends Filtered {
  playerId?: GUID;
  teamId?: GUID;
  matchId?: GUID;
  type?: StatisticType;
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
  matchId: GUID;

  /**
   * The ID of the player for whom the statistic is recorded.
   * @type {string}
   */
  playerId: GUID;

  /**
   * The type of statistic (Points or Assists).
   * @type {StatisticType}
   */
  type: StatisticType;
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
  id: GUID;

  /**
   * The ID of the player for whom the statistic is recorded.
   * @type {string}
   */
  playerId: GUID;

  /**
   * The value of the player statistic.
   * @type {number}
   */
  value: number;

  /**
   * The ID of the match in which the statistic was recorded.
   * @type {string}
   */
  matchId: GUID;

  /**
   * The type of statistic (Points or Assists).
   * @type {StatisticType}
   */
  type: StatisticType;

  /**
   * The date of the associated match, for display without a separate lookup.
   * @type {string | null}
   */
  matchDate: string | null;
}

/**
 * A single player's points within a team's match sheet (HU-71).
 * @interface PlayerScoreEntry
 */
export interface PlayerScoreEntry {
  /**
   * The player who scored.
   * @type {GUID}
   */
  playerId: GUID;

  /**
   * The points the player scored in the match (may be zero).
   * @type {number}
   */
  points: number;
}

/**
 * The request body structure for loading a whole team's match sheet
 * (planilla) in one call (HU-71). The sum of `scores` points must equal the
 * team's final score for the match.
 * @interface LoadMatchSheetRequest
 */
export interface LoadMatchSheetRequest {
  /**
   * The match whose sheet is being loaded.
   * @type {GUID}
   */
  matchId: GUID;

  /**
   * The team (home or visitor) whose players are being loaded.
   * @type {GUID}
   */
  teamId: GUID;

  /**
   * The per-player points for the team.
   * @type {PlayerScoreEntry[]}
   */
  scores: PlayerScoreEntry[];
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

export type PlayerStatisticsViewMode = 'team' | 'player';

export interface IPlayerStatisticCreatePageProps {
  open: boolean;
  onClose: () => void;
  onCreated?: () => void;
}

export interface IPlayerStatisticCreateFormState {
  value: string;
  type: StatisticType;
  tournamentId: GUID | '';
  divisionId: GUID | '';
  stageId: GUID | '';
  matchId: GUID | '';
  teamId: GUID | '';
  playerId: GUID | '';
}

export interface ITeamStatisticTableRow {
  id: string;
  teamName: string;
  playersWithScore: number;
  records: number;
  totalScore: number;
}

export interface IPlayerStatisticTableRow {
  id: GUID;
  playerId: GUID;
  playerName: string;
  teamName: string;
  records: number;
  totalScore: number;
}
