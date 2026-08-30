import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { GUID } from '@/modules/core/types/types';

/** A division bucket inside a tournament, holding its per-cup champions in order. */
export interface ChampionDivisionGroup {
  divisionName: string;
  entries: IChampionHistory[];
}

/** A tournament bucket inside a season, holding its divisions. Carries the
 *  tournament's category so the view can badge it. */
export interface ChampionTournamentGroup {
  tournamentId: GUID;
  tournamentName: string;
  category: TournamentCategory;
  divisions: ChampionDivisionGroup[];
}

/** A season bucket holding its tournaments. */
export interface ChampionSeasonGroup {
  seasonName: string;
  tournaments: ChampionTournamentGroup[];
}

/** Fallback label for tournaments not yet assigned to a season. */
const NO_SEASON_LABEL = 'Sin temporada';

/**
 * Shapes the flat champion history into the public page's hierarchy:
 * Season → Tournament → Division → per-cup champion entries. Order follows the
 * backend's ordering (first appearance fixes position at each level), so the
 * within-division entries keep the backend's tier order (Copa Oro before Copa
 * Plata). Entries with a null/empty season fall into a single "Sin temporada"
 * bucket. A tournament already implies its category, so category is carried on
 * the tournament rather than used as a grouping level.
 */
export const groupChampions = (
  history: IChampionHistory[]
): ChampionSeasonGroup[] => {
  const seasonOrder: string[] = [];
  const bySeason = new Map<string, ChampionSeasonGroup>();

  history.forEach(entry => {
    const seasonKey = entry.seasonName || NO_SEASON_LABEL;

    let season = bySeason.get(seasonKey);
    if (!season) {
      season = { seasonName: seasonKey, tournaments: [] };
      bySeason.set(seasonKey, season);
      seasonOrder.push(seasonKey);
    }

    let tournament = season.tournaments.find(
      t => t.tournamentId === entry.tournamentId
    );
    if (!tournament) {
      tournament = {
        tournamentId: entry.tournamentId,
        tournamentName: entry.tournamentName,
        category: entry.category,
        divisions: [],
      };
      season.tournaments.push(tournament);
    }

    let division = tournament.divisions.find(
      d => d.divisionName === entry.divisionName
    );
    if (!division) {
      division = { divisionName: entry.divisionName, entries: [] };
      tournament.divisions.push(division);
    }

    division.entries.push(entry);
  });

  return seasonOrder.map(seasonName => bySeason.get(seasonName)!);
};
