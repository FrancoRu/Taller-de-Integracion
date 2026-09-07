import { GUID } from '@/modules/core/types/types';

/**
 * One season (tournament) a club's team was registered in.
 * @interface IClubSeasonResponse
 */
export interface IClubSeasonResponse {
  /** The tournament (season) id. */
  tournamentId: GUID;
  /** The tournament (season) display name, when available. */
  tournamentName: string | null;
  /**
   * The tournament's start date (ISO string). Sort key only — the history
   * table shows the tournament name, and rows are ordered newest-first by
   * this value.
   */
  startDate: string;
}

/**
 * One per-season team belonging to a club, with the seasons it played.
 * @interface IClubTeamSeasonResponse
 */
export interface IClubTeamSeasonResponse {
  /** The per-season team id. */
  teamId: GUID;
  /** The team name for that season. */
  name: string;
  /** The team's URL-friendly slug. */
  slug: string;
  /** The three-letter code of the team. */
  threeLetterCode: string;
  /** The tournaments (seasons) this team was registered in. */
  seasons: IClubSeasonResponse[];
}

/**
 * A minimal club identity, used for pickers and for referencing a related
 * club (parent institution / child squad) without its full season history.
 * @interface IClubSummaryResponse
 */
export interface IClubSummaryResponse {
  id: GUID;
  name: string;
  slug: string;
  logoUrl: string | null;
}

/**
 * A club and its trajectory across seasons (HU-99): the stable club identity
 * plus every per-season team that belongs to it.
 * @interface IClubHistoryResponse
 */
export interface IClubHistoryResponse {
  /** The stable club id. */
  id: GUID;
  /** The club name. */
  name: string;
  /** The club's URL-friendly slug. */
  slug: string;
  /** The club logo URL, when available. */
  logoUrl: string | null;
  /** The per-season teams that make up this club's history. */
  teams: IClubTeamSeasonResponse[];
  /**
   * The parent institution this club is a squad of, or null when this club
   * has no parent linked.
   */
  parentClub: IClubSummaryResponse | null;
  /** Other squads linked to this club as their parent institution. */
  childClubs: IClubSummaryResponse[];
}

/**
 * Request body to clone a roster from a previous season's team (HU-53). The
 * target team is taken from the route; this identifies the source team +
 * season to copy from and the target season to copy into.
 * @interface IRosterCopyRequest
 */
export interface IRosterCopyRequest {
  /** The past-season team whose roster is the source. */
  sourceTeamId: GUID;
  /** The season (tournament) the source roster belongs to. */
  sourceTournamentId: GUID;
  /** The new season (tournament) the roster is cloned into. */
  targetTournamentId: GUID;
}

/**
 * Outcome of copying a roster into a new season (HU-53).
 * @interface IRosterCopyResult
 */
export interface IRosterCopyResult {
  /** New season registrations created on the target team. */
  copiedCount: number;
  /** Source players skipped because already registered to the target season. */
  skippedCount: number;
}

/**
 * Context properties for reading a club's cross-season history (HU-99) and
 * importing a roster from a previous season (HU-53).
 * @interface IClubContextProps
 */
export interface IClubContextProps {
  /** The last-fetched club history, or null. */
  club: IClubHistoryResponse | null;

  /**
   * Fetches a club and its per-season trajectory by id or slug.
   * @param idOrSlug The club's GUID id or its slug.
   * @returns A promise that resolves with the club history.
   */
  getClubHistory(idOrSlug: string): Promise<IClubHistoryResponse | void>;

  /**
   * Clones a roster from a previous season's team into a target team.
   * @param targetTeamId The team to copy the roster into.
   * @param request The source team + season and the target season.
   * @returns A promise that resolves with the copied/skipped counts.
   */
  copyRoster(
    targetTeamId: GUID,
    request: IRosterCopyRequest
  ): Promise<IRosterCopyResult | void>;

  /** Every club's stable identity summary, for the "link to parent club" picker. */
  allClubs: IClubSummaryResponse[];

  /** Fetches every club's stable identity summary. */
  getAllClubs(): Promise<IClubSummaryResponse[] | void>;

  /**
   * Links a club as a squad of a parent institution club.
   * @param childClubId The squad club to link.
   * @param parentClubId The institution club it becomes a squad of.
   */
  linkClubParent(
    childClubId: GUID,
    parentClubId: GUID
  ): Promise<IClubHistoryResponse | void>;

  /**
   * Clears a club's parent institution link, if any.
   * @param childClubId The club to unlink.
   */
  unlinkClubParent(childClubId: GUID): Promise<IClubHistoryResponse | void>;

  /**
   * Renames a club. The club's slug never changes, so its public URL stays
   * stable.
   * @param clubId The club to rename.
   * @param name The new display name.
   */
  renameClub(clubId: GUID, name: string): Promise<IClubHistoryResponse | void>;
}
