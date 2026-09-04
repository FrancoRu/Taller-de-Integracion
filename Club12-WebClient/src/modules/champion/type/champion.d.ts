import { GUID } from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';

/**
 * A single team occupying a podium place. Mirrors the backend's minimal team
 * projection (id + display name + optional logo) used for champions/podium.
 */
export interface IPodiumTeam {
  teamId: GUID;
  teamName: string;
  logoUrl: string | null;
}

/**
 * The top-three finish of a single division (HU-Champions). `first`/`second`/
 * `third` are `null` until that place is decided. `hasPlayoff` distinguishes a
 * podium crowned by a playoff bracket from one read straight off the final
 * standings — both are valid top-threes.
 */
export interface IPodium {
  divisionId: GUID;
  divisionName: string;
  hasPlayoff: boolean;
  first: IPodiumTeam | null;
  second: IPodiumTeam | null;
  third: IPodiumTeam | null;
}

/**
 * A single champion entry in the public history (only finished tournaments).
 * `category` is the raw backend enum name ("Masculine"/"Feminine") — display it
 * through `TOURNAMENT_CATEGORY_LABELS`.
 */
export interface IChampionHistory {
  tournamentId: GUID;
  tournamentName: string;
  seasonName: string | null;
  /**
   * The calendar year of the season, or null when there is no season or it
   * has no year. The public page sorts seasons by this value, newest first.
   */
  seasonYear: number | null;
  category: TournamentCategory;
  divisionName: string;
  /**
   * The sub-cup (playoff bracket) that was won, e.g. "Copa Oro" / "Copa Plata",
   * when the division splits its playoff into tiers. Null when the division
   * crowns a single champion (a single bracket or a group-only division).
   */
  cupName: string | null;
  championTeam: IPodiumTeam;
}
