import type { LibraryMatch, LibraryParticipant } from '@/modules/playoff/type/gLootBracketTypes.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { BracketModel, BracketRound } from '@/modules/playoff/type/bracket.d';
import { bracketParticipantId, bracketTeamLabel, isBracketBye, isBracketMatchWinner } from '@/modules/playoff/matchStatus';
import { translateStageType } from '@/modules/core/utils/translateStageType';

/** @g-loot/react-tournament-brackets match state for a decided seeding walkover. */
const STATE_WALKOVER = 'WALK_OVER';
/** @g-loot/react-tournament-brackets match state for a slot still missing at least one participant. */
const STATE_PENDING_PARTICIPANTS = 'NO_PARTY';
/** @g-loot/react-tournament-brackets match state for a completed, decided match or series. */
const STATE_DONE = 'DONE';
/** @g-loot/react-tournament-brackets match state for a fully-seeded match awaiting its result. */
const STATE_SCHEDULED = 'SCORE_DONE';

/**
 * A library `Match` carrying this app's original `IMatchResponse` alongside
 * it, so the custom match component ({@link BracketMatchLibraryAdapter})
 * can render the app's own card (team logos, series breakdown) instead of
 * the library's built-in one.
 */
export interface PlayoffLibraryMatch extends LibraryMatch {
  id: GUID;
  nextMatchId: GUID | null;
  raw: IMatchResponse;
  /**
   * The individual legs this node aggregates (chronologically ordered),
   * when `raw` collapses more than one raw `Match` row between the same
   * two teams — see `buildBracket.ts`'s tie grouping. Undefined for the
   * common case of one match per bracket slot.
   */
  legs?: IMatchResponse[];
}

const toParticipant = (match: IMatchResponse, side: 'home' | 'visitor'): LibraryParticipant => {
  const team = side === 'home' ? match.homeTeam : match.visitorTeam;
  return {
    id: bracketParticipantId(match, side),
    name: bracketTeamLabel(team, match),
    isWinner: isBracketMatchWinner(match, team?.id),
    resultText: match.isFinished && team ? String(team.score) : null,
  };
};

const matchState = (match: IMatchResponse): string => {
  if (isBracketBye(match)) return STATE_WALKOVER;
  if (!match.homeTeam || !match.visitorTeam) return STATE_PENDING_PARTICIPANTS;
  return match.isFinished ? STATE_DONE : STATE_SCHEDULED;
};

/**
 * Resolves the id of the library match a given round match should connect
 * into. Prefers buildBracket's own inferred edge — only ever present once a
 * winner is decided and the mapping is unambiguous (see
 * buildBracket.ts's "graceful degradation" rules). When that's absent (an
 * upcoming round whose participants aren't decided yet), falls back to
 * positional pairing (`index / 2` into the next round), matching the
 * backend's classic bracket seed order (see
 * `IStageContextProps.seedKnockoutStage`). Without this fallback, the
 * library — which derives its entire column layout by walking
 * `nextMatchId` backward from the final match — would silently drop any
 * not-yet-decided match from the rendered tree.
 */
const resolveNextMatchId = (
  matchIndex: number,
  edgeTarget: GUID | undefined,
  nextRound: BracketRound | undefined
): GUID | null => {
  if (edgeTarget) return edgeTarget;
  if (!nextRound || nextRound.matches.length === 0) return null;

  const fallbackIndex = Math.min(Math.floor(matchIndex / 2), nextRound.matches.length - 1);
  return nextRound.matches[fallbackIndex]?.id ?? null;
};

/**
 * A stand-in `IMatchResponse` for a round whose stage exists but has no
 * visible match rows (see {@link effectiveRounds}). Renders as a plain TBD
 * slot via the normal `BracketMatchNode` — no `id` collision risk since a
 * stage id is never reused as a match id.
 */
const placeholderRawMatch = (round: BracketRound): IMatchResponse => ({
  id: round.stageId,
  matchDate: '',
  matchType: 'Playoff' as IMatchResponse['matchType'],
  slug: '',
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  stageId: round.stageId,
});

/**
 * Rounds to actually feed the library, in bracket order. A single-elimination
 * bracket, by construction, always converges to exactly one final match; the
 * library's column layout depends on that (it walks `nextMatchId` backward
 * from the one match that has none). If a later round's stage exists but
 * currently has zero match rows — not yet generated, or simply not
 * returned by a paginated fetch — dropping it outright would leave *every*
 * match in the round before it with nowhere to point, i.e. multiple
 * `nextMatchId: null` "roots". Empirically, `SingleEliminationBracket`
 * doesn't render a proper multi-root forest — it silently drops every
 * root but one. So instead of dropping an empty round that has real rounds
 * before it, it's kept as a single synthetic TBD placeholder match, giving
 * every earlier match exactly one place to connect into. A *leading* empty
 * round (nothing before it yet) is dropped — there's nothing that needs to
 * point into it.
 */
const effectiveRounds = (model: BracketModel): BracketRound[] => {
  const result: BracketRound[] = [];
  let seenNonEmptyRound = false;

  for (const round of model.rounds) {
    if (round.matches.length > 0) {
      seenNonEmptyRound = true;
      result.push(round);
    } else if (seenNonEmptyRound) {
      result.push({ ...round, matches: [placeholderRawMatch(round)] });
    }
  }

  return result;
};

/**
 * Maps this app's `BracketModel` (rounds ordered by stage, plus the
 * connector edges `buildBracket` was able to infer) into the flat
 * `Match[]` shape `@g-loot/react-tournament-brackets`'s
 * `SingleEliminationBracket` expects. `thirdPlace` is intentionally
 * excluded — it renders as an unconnected side slot outside the library
 * (see `PlayoffBracket.tsx`). See {@link effectiveRounds} for how rounds
 * with no visible matches are handled.
 */
export function toLibraryMatches(model: BracketModel): PlayoffLibraryMatch[] {
  const rounds = effectiveRounds(model);
  const edgeTargetsBySource = new Map(model.edges.map(edge => [edge.fromMatchId, edge.toMatchId]));

  return rounds.flatMap((round, roundIndex) => {
    const nextRound = rounds[roundIndex + 1];
    const roundLabel = translateStageType(round.stageType).toUpperCase();

    // Every match stays in the array, bye or not: the library computes each
    // box's Y position — and every connector line — purely from
    // (rowIndex, columnIndex) assuming round N always has exactly twice
    // round N+1's match count (see calculate-match-position.js /
    // connectors.js). Dropping bye entries here shifts the surviving
    // matches' indices and desyncs that geometry from the actual bracket
    // shape — cards land at the wrong row, connectors point at the wrong
    // pair. A bye's card is hidden at render time instead (see
    // BracketMatchLibraryAdapter), which keeps every index — and therefore
    // every position — exactly where the library expects it.
    return round.matches.map((match, matchIndex) => ({
      id: match.id,
      nextMatchId: resolveNextMatchId(matchIndex, edgeTargetsBySource.get(match.id), nextRound),
      tournamentRoundText: roundLabel,
      startTime: match.matchDate,
      state: matchState(match),
      participants: [toParticipant(match, 'home'), toParticipant(match, 'visitor')],
      raw: match,
      legs: round.legsByMatchId?.get(match.id),
    }));
  });
}

/** Round header labels in column order, for the library's `roundTextGenerator`. */
export function libraryRoundLabels(model: BracketModel): string[] {
  return effectiveRounds(model).map(round => translateStageType(round.stageType).toUpperCase());
}
