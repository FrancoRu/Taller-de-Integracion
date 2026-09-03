import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';

/**
 * Wraps a standalone (best-of-1) playoff match in the same
 * `IMatchSeriesResponse` shape a real best-of-N series carries, so a
 * fixture list can render every playoff round through one `SeriesCard`
 * component instead of a bo1 round looking like a different, plainer UI
 * (a bare `MatchRow`) next to every BOx round's rich series card. Returns
 * `null` for a slot still missing a side (a TBD match awaiting a previous
 * round's winner) — there's no real matchup yet to present as a series.
 */
export const singleMatchAsSeries = (match: IMatchResponse): IMatchSeriesResponse | null => {
  if (!match.homeTeam || !match.visitorTeam) return null;

  return {
    id: match.id,
    // stageId is never read for display (SeriesCard doesn't use it) — the
    // fallback only exists to satisfy the type when a match's own stageId
    // is somehow null.
    stageId: match.stageId ?? match.id,
    homeTeamId: match.homeTeam.id,
    homeTeamName: match.homeTeam.name,
    visitorTeamId: match.visitorTeam.id,
    visitorTeamName: match.visitorTeam.name,
    bestOf: 1,
    winningTeamId: match.winningTeamId,
    winningTeamName: match.winningTeamName,
    games: [
      {
        id: match.id,
        matchDate: match.matchDate,
        homeTeamName: match.homeTeam.name,
        visitorTeamName: match.visitorTeam.name,
        homeScore: match.homeTeam.score,
        visitorScore: match.visitorTeam.score,
        winningTeamName: match.winningTeamName,
        isFinished: match.isFinished,
        matchType: match.matchType,
        status: match.status,
        round: match.round,
        gameNumber: 1,
      },
    ],
  };
};

/**
 * A match is a walkover (bye) when it was seeded with only one side present
 * and is already finished — the other side never had an opponent, as
 * opposed to a not-yet-seeded slot still waiting on a previous round's
 * winner.
 */
export const isBracketBye = (match: IMatchResponse): boolean =>
  match.isFinished && Boolean(match.homeTeam) !== Boolean(match.visitorTeam);

/**
 * Whether `teamId` is the recorded winner of `match`. Always false while
 * the match is unfinished or undecided.
 */
export const isBracketMatchWinner = (match: IMatchResponse, teamId?: string | null): boolean =>
  Boolean(match.isFinished && match.winningTeamId && teamId && match.winningTeamId === teamId);

/**
 * A bracket slot still awaiting a previous round's winner on at least one
 * side (as opposed to a bye, which is already decided).
 */
export const isBracketPending = (match: IMatchResponse): boolean =>
  !isBracketBye(match) && (!match.homeTeam || !match.visitorTeam);

/**
 * Renders a team's display label for a bracket slot: the team name once
 * assigned, "BYE" once a walkover has been decided, or "A definir" (TBD)
 * while still awaiting a previous round's winner.
 */
export const bracketTeamLabel = (
  team: IMatchResponse['homeTeam'],
  match: IMatchResponse
): string => {
  if (team) return team.name;
  return isBracketBye(match) ? 'BYE' : 'A definir';
};

/** A stable, unique participant id for a bracket slot with no team assigned yet. */
export const unresolvedParticipantId = (match: IMatchResponse, side: 'home' | 'visitor'): string =>
  `${match.id}:${side}`;

/** The participant id the bracket library should use for one side of a match. */
export const bracketParticipantId = (
  match: IMatchResponse,
  side: 'home' | 'visitor'
): string => {
  const team = side === 'home' ? match.homeTeam : match.visitorTeam;
  return team?.id ?? unresolvedParticipantId(match, side);
};

export type BracketSide = 'home' | 'visitor';

/**
 * Sums each team's score across every leg of a multi-leg tie (e.g. a
 * historical home-and-away semifinal recorded as separate `Match` rows
 * rather than a `BestOf` `MatchSeries`), keyed by team id rather than by
 * home/visitor slot — legs commonly swap which side is "home". Only
 * finished legs contribute to either total.
 */
export const aggregateLegScores = (legs: IMatchResponse[]): Map<GUID, number> => {
  const totals = new Map<GUID, number>();

  for (const leg of legs) {
    if (!leg.isFinished) continue;
    for (const team of [leg.homeTeam, leg.visitorTeam]) {
      if (!team) continue;
      totals.set(team.id, (totals.get(team.id) ?? 0) + team.score);
    }
  }

  return totals;
};

/**
 * Decides the winner of a multi-leg tie by aggregate score — extending
 * {@link isBracketMatchWinner}'s single-match "isFinished && winningTeamId"
 * rule to a tie of N legs instead of forking a parallel decision rule.
 * Returns null while any leg is still unplayed, or if the aggregate is
 * level (undecided even though every leg has been played).
 */
export const aggregateTieWinner = (
  legs: IMatchResponse[]
): { winningTeamId: GUID; winningTeamName: string } | null => {
  if (legs.length === 0 || legs.some(leg => !leg.isFinished)) return null;

  const totals = aggregateLegScores(legs);
  const [first, second] = [...totals.entries()];
  if (!first || !second || first[1] === second[1]) return null;

  const [winningTeamId] = first[1] > second[1] ? first : second;
  const winningTeam = legs
    .flatMap(leg => [leg.homeTeam, leg.visitorTeam])
    .find(team => team?.id === winningTeamId);

  return winningTeam ? { winningTeamId: winningTeam.id, winningTeamName: winningTeam.name } : null;
};

/**
 * One team's score in each finished game of a best-of-N series, in game
 * order — so a bracket card can show "Juego 1, Juego 2, …" results at a
 * glance instead of only the aggregate. Games carry team NAMES rather than
 * ids (see `ISeriesGameResponse`), and a series can swap which side is
 * "home" from game to game, so each game is matched by name rather than by
 * home/visitor slot. An unplayed game is skipped, not padded with a
 * placeholder.
 */
export const seriesGameScores = (
  series: IMatchSeriesResponse,
  teamName: string
): number[] =>
  [...series.games]
    .filter(game => game.isFinished)
    .sort((a, b) => a.gameNumber - b.gameNumber)
    .map(game => {
      if (game.homeTeamName === teamName) return game.homeScore ?? 0;
      if (game.visitorTeamName === teamName) return game.visitorScore ?? 0;
      return 0;
    });

/**
 * One team's score in each finished leg of a multi-leg tie (no `MatchSeries`
 * behind it), in chronological order — the `legs` array's own order, same
 * as {@link aggregateLegScores}. Legs are already keyed by team id, unlike
 * series games, since legs carry the full `IMatchResponse` team objects.
 */
export const legGameScores = (legs: IMatchResponse[], teamId: GUID): number[] =>
  legs
    .filter(leg => leg.isFinished)
    .map(leg => {
      if (leg.homeTeam?.id === teamId) return leg.homeTeam.score;
      if (leg.visitorTeam?.id === teamId) return leg.visitorTeam.score;
      return 0;
    });
