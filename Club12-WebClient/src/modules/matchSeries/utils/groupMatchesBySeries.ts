import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';

/** One or more matches that belong together: either every game of a
 * best-of-N series (`series` set), or a single standalone match
 * (`series` is `null`, `matches` has exactly one entry). */
export interface MatchGroup {
  series: IMatchSeriesResponse | null;
  matches: IMatchResponse[];
}

/**
 * Groups a flat match list so a best-of-N series' individual games stay
 * together instead of being interleaved with other pairs' games under the
 * same stage/round (e.g. two semifinal series both playing on the same
 * dates). `IMatchResponse` carries no `seriesId` of its own, so membership
 * is derived from `seriesById` — every game listed on a series' `games`
 * array maps back to that series. Order is preserved: each group appears
 * at the position of its first match.
 */
export function groupMatchesBySeries(
  matches: IMatchResponse[],
  seriesById: Map<GUID, IMatchSeriesResponse>
): MatchGroup[] {
  const seriesIdByMatchId = new Map<GUID, GUID>();
  for (const series of seriesById.values()) {
    for (const game of series.games) {
      seriesIdByMatchId.set(game.id, series.id);
    }
  }

  const groups = new Map<string, MatchGroup>();
  const order: string[] = [];

  for (const match of matches) {
    const seriesId = seriesIdByMatchId.get(match.id);
    const key = seriesId ?? `single:${match.id}`;

    let group = groups.get(key);
    if (!group) {
      group = { series: seriesId ? (seriesById.get(seriesId) ?? null) : null, matches: [] };
      groups.set(key, group);
      order.push(key);
    }
    group.matches.push(match);
  }

  return order.map(key => groups.get(key)!);
}
