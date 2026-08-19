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
