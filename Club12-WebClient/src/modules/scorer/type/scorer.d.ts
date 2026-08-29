import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { MatchFiltered } from '@/modules/match/type/match';

export interface IScorerContextProps {
  scorersByTeam: IScorerByTeamResponse[] | null;
  scorersByPlayer: IScorerByPlayerResponse[] | null;

  getScorersByTeamFiltered(
    filter: IScorerByTeamFiltered
  ): Promise<GenericResponsePagination<IScorerByTeamResponse> | void>;

  getScorersByPlayerFiltered(
    filter: IScorerFiltered
  ): Promise<GenericResponsePagination<IScorerByPlayerResponse> | void>;
}

export interface IScorerFiltered extends Filtered {
  tournamentId?: GUID;
  /** Scopes the ranking to one division (a zone or the cross-division cup) — every stage in it. */
  divisionId?: GUID;
  /** Scopes the ranking to a single stage (e.g. just the group phase, or one named playoff bracket's round). */
  stageId?: GUID;
  matchId?: GUID;
  teamId?: GUID;
  playerId?: GUID;
  /**
   * Scopes the goleadores ranking to a whole SEASON (HU-85) — the calendar year
   * of a tournament's start date. Independent from `tournamentId`. Leaving both
   * `tournamentId` and `season` unset yields the ALL-TIME ranking.
   */
  season?: number;
}

/**
 * The three HU-85 ranking scopes a goleadores view can switch between:
 * a single tournament, a whole season (calendar year), or all-time.
 */
export type ScorerScope = 'tournament' | 'season' | 'allTime';

export type IScorerByTeamFiltered = MatchFiltered;

export interface IScorerBaseResponse {
  points: number;
}

export interface IScorerByPlayerResponse extends IScorerBaseResponse {
  playerId: GUID;
  fullName: string;
  /** The player's jersey number (dorsal), when known — for the match kit. */
  jerseyNumber?: number | null;
}

export interface IScorerByTeamResponse extends IScorerBaseResponse {
  teamId: GUID;
  name: string;
}

export type ScorersViewMode = 'team' | 'player';
