import type { LibraryMatchComponentProps } from '@/modules/playoff/type/gLootBracketTypes.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { PlayoffLibraryMatch } from '@/modules/playoff/bracketAdapter';
import BracketMatchNode from '@/views/playoff/BracketMatchNode';

interface BracketMatchLibraryAdapterProps extends Pick<LibraryMatchComponentProps, 'match'> {
  /** Maps a bracket node's id (the MatchSeries id, for BestOf > 1 rounds) to its full series data. */
  seriesById?: Map<GUID, IMatchSeriesResponse>;
}

/**
 * Bridges `@g-loot/react-tournament-brackets`'s `matchComponent` slot to
 * this app's own `BracketMatchNode` card, so the bracket keeps its team
 * logos and best-of-N series breakdown instead of falling back to the
 * library's default look. Ignores every layout/highlight prop the library
 * computes (`topParty`, `connectorColor`, etc.) — `BracketMatchNode`
 * derives everything it needs straight from the original `IMatchResponse`
 * carried on `match.raw` by {@link toLibraryMatches}.
 */
export default function BracketMatchLibraryAdapter({ match, seriesById }: BracketMatchLibraryAdapterProps) {
  const { raw, legs } = match as unknown as PlayoffLibraryMatch;
  return <BracketMatchNode match={raw} series={seriesById?.get(raw.id)} legs={legs} />;
}
