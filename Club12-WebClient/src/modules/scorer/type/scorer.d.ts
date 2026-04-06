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
  divisionId?: GUID;
  stageId?: GUID;
  matchId?: GUID;
  teamId?: GUID;
  playerId?: GUID;
}

export type IScorerByTeamFiltered = MatchFiltered;

export interface IScorerBaseResponse {
  points: number;
}

export interface IScorerByPlayerResponse extends IScorerBaseResponse {
  playerId: GUID;
  fullName: string;
}

export interface IScorerByTeamResponse extends IScorerBaseResponse {
  teamId: GUID;
  name: string;
}

export type ScorersViewMode = 'team' | 'player';
