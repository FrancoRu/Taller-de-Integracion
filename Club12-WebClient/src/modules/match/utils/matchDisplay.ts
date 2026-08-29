import { MatchStatus } from '@/modules/core/enum/match/matchStatus';

export const MATCH_FINISHED_LABEL = 'Finalizado';
export const MATCH_SCHEDULED_LABEL = 'Programado';

export const MATCH_FINISHED_COLOR = 'success' as const;
export const MATCH_SCHEDULED_COLOR = 'default' as const;

export const getMatchStatusLabel = (finished: boolean): string =>
  finished ? MATCH_FINISHED_LABEL : MATCH_SCHEDULED_LABEL;

export const getMatchStatusColor = (finished: boolean) =>
  finished ? MATCH_FINISHED_COLOR : MATCH_SCHEDULED_COLOR;

const WALKOVER_LABEL = 'W.O.';
const WALKOVER_WINNING_SCORE = 20;
const WALKOVER_LOSING_SCORE = 0;

/**
 * The league's walkover (forfeit) convention: the no-show team is recorded
 * as 0, the present team as 20. No team can actually score 0 in a played
 * game, so this pattern is an unambiguous signal, not a real scoreline.
 */
export const isWalkoverScore = (homeScore: number, visitorScore: number): boolean =>
  (homeScore === WALKOVER_WINNING_SCORE && visitorScore === WALKOVER_LOSING_SCORE) ||
  (homeScore === WALKOVER_LOSING_SCORE && visitorScore === WALKOVER_WINNING_SCORE);

export const formatMatchScore = (homeScore: number, visitorScore: number): string =>
  isWalkoverScore(homeScore, visitorScore)
    ? WALKOVER_LABEL
    : `${homeScore} – ${visitorScore}`;

/** MUI Chip color for each match status. */
type MatchStatusColor = 'default' | 'success' | 'warning' | 'info';

const MATCH_STATUS_LABELS: Record<MatchStatus, string> = {
  [MatchStatus.Scheduled]: 'Programado',
  [MatchStatus.Played]: 'Jugado',
  [MatchStatus.Suspended]: 'Suspendido',
  [MatchStatus.WalkOver]: 'W.O.',
};

const MATCH_STATUS_COLORS: Record<MatchStatus, MatchStatusColor> = {
  [MatchStatus.Scheduled]: 'default',
  [MatchStatus.Played]: 'success',
  [MatchStatus.Suspended]: 'warning',
  [MatchStatus.WalkOver]: 'info',
};

/**
 * Resolves the effective match status, falling back to a value derived from
 * `isFinished` when the backend did not populate `status` (older responses).
 */
export const resolveMatchStatus = (
  status: MatchStatus | null | undefined,
  isFinished: boolean
): MatchStatus =>
  status ?? (isFinished ? MatchStatus.Played : MatchStatus.Scheduled);

/** The Spanish label shown on the status badge. */
export const getMatchStatusBadgeLabel = (
  status: MatchStatus | null | undefined,
  isFinished: boolean
): string => MATCH_STATUS_LABELS[resolveMatchStatus(status, isFinished)];

/** The MUI Chip color for the status badge. */
export const getMatchStatusBadgeColor = (
  status: MatchStatus | null | undefined,
  isFinished: boolean
): MatchStatusColor => MATCH_STATUS_COLORS[resolveMatchStatus(status, isFinished)];

/** How prominently one side of the scoreboard should read. */
export type ScoreEmphasis = 'winner' | 'loser' | 'neutral';

/** The emphasis to apply to each side of a scoreboard. */
export interface ScoreboardEmphasis {
  home: ScoreEmphasis;
  visitor: ScoreEmphasis;
}

/**
 * Derives which side of the scoreboard is the winner (emphasised) and which is
 * the loser (dimmed), from the match's recorded winner. A match that is not
 * finished — or finished without a recorded winner — stays neutral on both
 * sides, so no team is emphasised before there is a real result. The league
 * plays on neutral venues (HU: no local/visita), so this is purely about the
 * result, never home/away standing.
 */
export const getScoreboardEmphasis = (params: {
  isFinished: boolean;
  homeTeamId?: string | null;
  visitorTeamId?: string | null;
  winningTeamId?: string | null;
}): ScoreboardEmphasis => {
  const { isFinished, homeTeamId, visitorTeamId, winningTeamId } = params;

  if (!isFinished || !winningTeamId) {
    return { home: 'neutral', visitor: 'neutral' };
  }
  if (winningTeamId === homeTeamId) {
    return { home: 'winner', visitor: 'loser' };
  }
  if (winningTeamId === visitorTeamId) {
    return { home: 'loser', visitor: 'winner' };
  }
  return { home: 'neutral', visitor: 'neutral' };
};

/**
 * Orders a team's match scorers for display: most points first, ties broken by
 * name so the list is stable. Returns a new array — the input is never mutated.
 */
export const sortScorersByPoints = <T extends { points: number; fullName: string }>(
  scorers: T[]
): T[] =>
  [...scorers].sort(
    (a, b) => b.points - a.points || a.fullName.localeCompare(b.fullName)
  );
