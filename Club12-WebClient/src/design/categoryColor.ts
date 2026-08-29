import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { luminance } from './colorName';
import { category } from './tokens';

/**
 * The single source for category branding hues (masculine -> orange, feminine
 * -> purple) plus the ink that stays legible on top. Chips and section accents
 * read from here so the masculine/feminine visual language is defined once and
 * never drifts between surfaces.
 */
export interface CategoryColor {
  /** The category's brand fill (#rrggbb). */
  fill: string;
  /** A contrasting ink (near-black on light fills, off-white on dark ones). */
  ink: string;
}

/** Resolves a tournament category into its brand fill plus a legible ink. */
export const categoryColor = (cat: TournamentCategory): CategoryColor => {
  const fill =
    cat === TournamentCategory.Feminine ? category.feminine : category.masculine;
  // Mirror resolveShirtColor's threshold/ink so contrast stays consistent.
  return { fill, ink: luminance(fill) > 0.55 ? '#0b0f17' : '#f5f5f5' };
};
