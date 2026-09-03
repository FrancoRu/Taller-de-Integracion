import type { LibraryMatchComponentProps } from '@/modules/playoff/type/gLootBracketTypes.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { PlayoffLibraryMatch } from '@/modules/playoff/bracketAdapter';
import { isBracketBye } from '@/modules/playoff/matchStatus';
import { resolveClickTargetMatchId } from '@/modules/playoff/bracketMatchNavigation';
import BracketMatchNode from '@/views/playoff/BracketMatchNode';

interface BracketMatchLibraryAdapterProps extends Pick<LibraryMatchComponentProps, 'match'> {
  /** Maps a bracket node's id (the MatchSeries id, for BestOf > 1 rounds) to its full series data. */
  seriesById?: Map<GUID, IMatchSeriesResponse>;
  /** See {@link resolveClickTargetMatchId} — omitted in read-only contexts. */
  onMatchClick?: (matchId: GUID) => void;
}

/**
 * Bridges `@g-loot/react-tournament-brackets`'s `matchComponent` slot to
 * this app's own `BracketMatchNode` card, so the bracket keeps its team
 * logos and best-of-N series breakdown instead of falling back to the
 * library's default look. Ignores every layout/highlight prop the library
 * computes (`topParty`, `connectorColor`, etc.) — `BracketMatchNode`
 * derives everything it needs straight from the original `IMatchResponse`
 * carried on `match.raw` by {@link toLibraryMatches}.
 *
 * A bye's slot stays in the library's match array (so every other card's
 * position and every connector line stays correctly aligned — see
 * `toLibraryMatches`), but renders nothing here: the lone team already
 * shows up one column over with the walkover behind it, so a card for the
 * bye itself would only repeat that team's name next to a "BYE" label.
 */
export default function BracketMatchLibraryAdapter({
  match,
  seriesById,
  onMatchClick,
}: BracketMatchLibraryAdapterProps) {
  const { raw, legs } = match as unknown as PlayoffLibraryMatch;

  if (isBracketBye(raw)) return null;

  const series = seriesById?.get(raw.id);
  const targetId = onMatchClick ? resolveClickTargetMatchId(raw, series, legs) : undefined;

  return (
    <BracketMatchNode
      match={raw}
      series={series}
      legs={legs}
      onClick={targetId ? () => onMatchClick!(targetId) : undefined}
    />
  );
}
