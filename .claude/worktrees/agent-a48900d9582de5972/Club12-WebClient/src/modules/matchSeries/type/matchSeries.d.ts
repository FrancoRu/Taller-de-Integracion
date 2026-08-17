import { Filtered, GUID } from '@/modules/core/types/types';
import { IMinimalMatchResponse } from '@/modules/match/type/match.d';

/**
 * A single game within a best-of-N series, including its position in the
 * series alongside the usual minimal match details.
 */
export interface ISeriesGameResponse extends IMinimalMatchResponse {
  gameNumber: number;
}

/**
 * A best-of-N playoff series between two teams at one bracket round,
 * including its individual games.
 */
export interface IMatchSeriesResponse {
  id: GUID;
  stageId: GUID;
  homeTeamId: GUID;
  homeTeamName: string;
  visitorTeamId: GUID;
  visitorTeamName: string;
  bestOf: number;
  winningTeamId: GUID | null;
  winningTeamName: string | null;
  games: ISeriesGameResponse[];
}

/**
 * The request body structure for creating a new best-of-N series between
 * two teams at a stage.
 */
export interface IAddMatchSeriesRequest {
  stageId: GUID;
  homeTeamId: GUID;
  visitorTeamId: GUID;
}

/**
 * The request body structure for scheduling the next game of an existing
 * series.
 */
export interface IAddGameToSeriesRequest {
  matchDate: string;
  venueId?: GUID;
}

/**
 * The filter criteria for fetching series.
 */
export interface MatchSeriesFiltered extends Filtered {
  stageId?: GUID;
}
