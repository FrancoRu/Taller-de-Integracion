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
