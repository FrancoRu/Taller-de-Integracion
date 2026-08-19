import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { StageType } from '@/modules/stage/type/stage';

/**
 * A single inferred connector between a source match and the next-round
 * match its winner advances into. Only emitted when the mapping is
 * unambiguous — see buildBracket's degradation rules.
 */
export interface BracketEdge {
  fromMatchId: GUID;
  toMatchId: GUID;
}

/**
 * One column of the bracket tree: all matches belonging to a single
 * elimination stage (e.g. all Semifinal matches for a division).
 */
export interface BracketRound {
  stageId: GUID;
  stageType: StageType;
  matches: IMatchResponse[];
  /**
   * Maps a bracket node's id to its individual legs (chronologically
   * ordered) when that node aggregates more than one raw `Match` row
   * between the same two teams in this round — e.g. a historical
   * home-and-away tie with no `MatchSeries` behind it (see
   * `buildBracket.ts`'s tie grouping). Optional and absent/undefined for
   * the common case of one match per bracket slot, so existing
   * `BracketRound` literals (tests, callers) stay valid without it.
   */
  legsByMatchId?: Map<GUID, IMatchResponse[]>;
}

/**
 * The full client-side bracket model for a division, ready to render.
 * `rounds` is ordered QuarterFinal -> SemiFinal -> Final. `thirdPlace` is a
 * side slot rendered next to the Final, not chained into the main path.
 * `edges` only contains unambiguous connectors; the model is always valid
 * (never throws) even when no connectors can be inferred.
 */
export interface BracketModel {
  rounds: BracketRound[];
  thirdPlace?: BracketRound;
  edges: BracketEdge[];
}

/**
 * One named parallel bracket within a division (e.g. an admin-defined
 * "Copa de Oro" / "Copa de Plata" pair, or a single unnamed bracket when
 * the division only has one elimination path). `bracketName` is free
 * text, entirely admin-defined; null means the stage(s) carry no
 * BracketName and form the division's single/default bracket.
 */
export interface BracketGroup {
  bracketName: string | null;
  model: BracketModel;
}
