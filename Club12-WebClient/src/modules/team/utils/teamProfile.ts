import { TeamMatch, TeamMatchResult } from '@/modules/team/type/teamProfile.d';

/** How many recent results the streak/"Últimos" blocks show. */
const RECENT_LIMIT = 5;

/** A finished match that actually carries a W/L result. */
const isDecided = (
  match: TeamMatch
): match is TeamMatch & { result: TeamMatchResult } =>
  match.isFinished && match.result !== null;

/**
 * The team's last {@link RECENT_LIMIT} finished results, oldest first so the row
 * reads left-to-right in time order (e.g. `['W','W','L','W','L']`). Input is
 * expected date-ascending (the matches endpoint's order); unfinished matches and
 * finished ones missing a result are ignored.
 */
export const deriveStreak = (matches: TeamMatch[]): TeamMatchResult[] =>
  matches
    .filter(isDecided)
    .slice(-RECENT_LIMIT)
    .map(match => match.result);

/**
 * Splits a fixture into the two lists the team page renders:
 * - `upcoming`: matches not yet finished, kept in ascending (nearest-first) order.
 * - `recent`: the {@link RECENT_LIMIT} most recently finished matches, newest first.
 * Input is expected date-ascending (the matches endpoint's order).
 */
export const splitFixture = (
  matches: TeamMatch[]
): { upcoming: TeamMatch[]; recent: TeamMatch[] } => {
  const upcoming = matches.filter(match => !match.isFinished);
  const recent = matches
    .filter(match => match.isFinished)
    .slice(-RECENT_LIMIT)
    .reverse();

  return { upcoming, recent };
};

/** A team's aggregated season record across all of its finished matches. */
export interface TeamRecord {
  wins: number;
  losses: number;
  played: number;
  pointsFor: number;
  pointsAgainst: number;
  pointsDifference: number;
}

/**
 * Aggregates a team's full record from ALL of its finished matches (group stage
 * AND playoffs), not just the group-stage standing. This keeps the headline
 * record, points-for/against and differential consistent with the streak and
 * fixture the visitor sees right below them — a standing only counts group-stage
 * games, so a team that also played playoffs would otherwise show fewer wins
 * than it actually has. Matches missing a score are still counted as played but
 * contribute no points.
 */
export const computeRecord = (matches: TeamMatch[]): TeamRecord => {
  const finished = matches.filter(match => match.isFinished);

  return finished.reduce<TeamRecord>(
    (record, match) => {
      const pointsFor = record.pointsFor + (match.teamScore ?? 0);
      const pointsAgainst = record.pointsAgainst + (match.opponentScore ?? 0);
      return {
        wins: record.wins + (match.result === 'W' ? 1 : 0),
        losses: record.losses + (match.result === 'L' ? 1 : 0),
        played: record.played + 1,
        pointsFor,
        pointsAgainst,
        pointsDifference: pointsFor - pointsAgainst,
      };
    },
    { wins: 0, losses: 0, played: 0, pointsFor: 0, pointsAgainst: 0, pointsDifference: 0 }
  );
};

/** Formats a win-loss record as `"5-2"`. */
export const formatRecord = (wins: number, losses: number): string =>
  `${wins}-${losses}`;

/** Formats a 1-based table position with the Spanish ordinal marker, e.g. `"3º"`. */
export const formatPosition = (position: number): string => `${position}º`;

/**
 * Formats a points differential with an explicit sign: `"+12"`, `"0"`, `"-5"`.
 * A non-negative differential reads as a positive (green) tone on the page.
 */
export const formatDifferential = (difference: number): string =>
  difference > 0 ? `+${difference}` : `${difference}`;
