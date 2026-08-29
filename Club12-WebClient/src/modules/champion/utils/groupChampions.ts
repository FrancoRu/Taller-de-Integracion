import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';

/** A category bucket inside a season, holding its champion entries in order. */
export interface ChampionCategoryGroup {
  category: TournamentCategory;
  entries: IChampionHistory[];
}

/** A season bucket holding its present category subsections. */
export interface ChampionSeasonGroup {
  seasonName: string;
  categories: ChampionCategoryGroup[];
}

/** Fallback label for tournaments not yet assigned to a season. */
const NO_SEASON_LABEL = 'Sin temporada';

/**
 * Category render order: masculine competition first, then feminine. Only
 * categories that actually have entries are emitted, so a season with a single
 * category never shows an empty subsection.
 */
const CATEGORY_ORDER: TournamentCategory[] = [
  TournamentCategory.Masculine,
  TournamentCategory.Feminine,
];

/**
 * Shapes the flat champion history into a two-level hierarchy for the public
 * page: Season -> Category -> entries. Season order follows the backend's
 * ordering (the first time a season appears fixes its position); entries with a
 * null/empty season fall into a single "Sin temporada" bucket. Within a season,
 * categories are ordered masculine-then-feminine and only present categories are
 * kept. Entry order inside each category mirrors the input order.
 */
export const groupChampions = (
  history: IChampionHistory[]
): ChampionSeasonGroup[] => {
  const seasonOrder: string[] = [];
  const bySeason = new Map<string, Map<TournamentCategory, IChampionHistory[]>>();

  history.forEach(entry => {
    const seasonKey = entry.seasonName || NO_SEASON_LABEL;
    let categories = bySeason.get(seasonKey);
    if (!categories) {
      categories = new Map();
      bySeason.set(seasonKey, categories);
      seasonOrder.push(seasonKey);
    }

    const existing = categories.get(entry.category);
    if (existing) {
      existing.push(entry);
    } else {
      categories.set(entry.category, [entry]);
    }
  });

  return seasonOrder.map(seasonName => {
    const categories = bySeason.get(seasonName);
    return {
      seasonName,
      categories: CATEGORY_ORDER.flatMap(category => {
        const entries = categories?.get(category);
        return entries ? [{ category, entries }] : [];
      }),
    };
  });
};
