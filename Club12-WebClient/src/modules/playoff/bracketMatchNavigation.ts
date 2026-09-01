import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';

/**
 * Resolves the real, navigable `Match` id a bracket card's click should open
 * — never the card's own `raw.id`, which for a BestOf > 1 round is a
 * synthetic `MatchSeries` id and for a historical tie (`buildBracket.ts`'s
 * `buildTieMatch`) is a synthetic `tie:${stageId}:${pairKey}` string, neither
 * of which is a real `Match` row.
 *
 * - Series node: the first unfinished game, or the last game if the series
 *   is already decided. Returns `undefined` if the series has no games yet
 *   (nothing to navigate to — series/game creation has no admin UI, #35).
 * - Tie node: same first-unfinished/else-last rule over the individual legs.
 * - Plain match: `raw.id` is already a real `Match` row.
 *
 * Also returns `undefined` while either side of the matchup is still TBD
 * (a future round awaiting a previous winner) — there is nothing useful to
 * edit on a match with no teams assigned yet.
 */
export const resolveClickTargetMatchId = (
  raw: IMatchResponse,
  series: IMatchSeriesResponse | undefined,
  legs: IMatchResponse[] | undefined
): GUID | undefined => {
  if (!raw.homeTeam || !raw.visitorTeam) {
    return undefined;
  }

  if (series) {
    const nextGame = series.games.find(game => !game.isFinished);
    return (nextGame ?? series.games[series.games.length - 1])?.id;
  }

  if (legs && legs.length > 1) {
    const nextLeg = legs.find(leg => !leg.isFinished);
    return (nextLeg ?? legs[legs.length - 1])?.id;
  }

  return raw.id;
};
