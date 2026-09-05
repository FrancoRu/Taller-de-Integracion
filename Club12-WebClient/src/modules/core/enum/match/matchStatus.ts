/**
 * The lifecycle status of a match (HU-69/HU-73). Mirrors the backend
 * `Domain.Enums.MatchStatus` and is serialized as a string on the match
 * response DTOs.
 * @enum MatchStatus
 */
export enum MatchStatus {
  /**
   * The match has a fixture but no result loaded yet.
   */
  Scheduled = 'Scheduled',

  /**
   * A normal result was loaded (HU-69); the match has a winner.
   */
  Played = 'Played',

  /**
   * The match was suspended (HU-68/HU-73) and awaits rescheduling.
   */
  Suspended = 'Suspended',

  /**
   * A walkover was applied (HU-73): the present team was awarded the
   * regulation default result. Distinguishable from a normal `Played`.
   */
  WalkOver = 'WalkOver',

  /**
   * The match will never be played: its tournament was canceled, or
   * force-closed as finished while the match was still pending.
   */
  Canceled = 'Canceled',
}
