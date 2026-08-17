import { IMatchResponse } from '@/modules/match/type/match.d';

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
