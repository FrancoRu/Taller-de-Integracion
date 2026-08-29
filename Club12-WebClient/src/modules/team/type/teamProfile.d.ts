import { GUID } from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';

/**
 * The public "team profile" contract — the standing, fixture and participation
 * data behind a team's public page. These mirror the backend projections served
 * from `/api/teams/{idOrSlug}/…` and are read-only.
 */

/**
 * A team's standing within one division of one tournament. Served by
 * `GET /api/teams/{idOrSlug}/summary?tournamentId=`; the endpoint answers `null`
 * when the team has no standing yet (e.g. a fixture that has not started).
 */
export interface TeamSummary {
  divisionId: GUID;
  divisionName: string;
  /** The team's 1-based position in its division's table. */
  position: number;
  /** How many teams share the division (the "de N" in "3º de 8"). */
  totalTeams: number;
  played: number;
  wins: number;
  losses: number;
  pointsFor: number;
  pointsAgainst: number;
  /** `pointsFor - pointsAgainst`; may be negative. */
  pointsDifference: number;
  /** Table points (the league's own scoring, not basketball points). */
  points: number;
}

/** A finished team match's outcome from the team's own perspective. */
export type TeamMatchResult = 'W' | 'L';

/**
 * One match on a team's fixture, oriented to the team whose page is shown.
 * Served (ordered by date ascending) by
 * `GET /api/teams/{idOrSlug}/matches?tournamentId=`.
 */
export interface TeamMatch {
  matchId: GUID;
  /** ISO date-time, or `null` when the match is not scheduled yet. */
  matchDate: string | null;
  isFinished: boolean;
  /** Raw backend status string (kept opaque; used only for display). */
  status: string;
  /** Whether this team plays at home. */
  isHome: boolean;
  opponentTeamId: GUID;
  opponentName: string;
  opponentLogoUrl: string | null;
  /** This team's score, or `null` until the match is finished. */
  teamScore: number | null;
  opponentScore: number | null;
  /** `'W'`/`'L'` for a finished match, `null` otherwise. */
  result: TeamMatchResult | null;
  venueName: string | null;
}

/**
 * One tournament this team has taken part in. Served (newest first) by
 * `GET /api/teams/{idOrSlug}/participations`. `isCurrent` marks the ongoing
 * tournament, used as the default selection on the team page.
 */
export interface TeamParticipation {
  tournamentId: GUID;
  tournamentName: string;
  tournamentSlug: string | null;
  category: TournamentCategory;
  seasonId: GUID | null;
  seasonName: string | null;
  year: number | null;
  isCurrent: boolean;
}
