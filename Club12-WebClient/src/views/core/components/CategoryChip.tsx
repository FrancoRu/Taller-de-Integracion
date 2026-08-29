import { Chip } from '@mui/material';
import { categoryColor } from '@/design/categoryColor';
import {
  TOURNAMENT_CATEGORY_LABELS,
  TournamentCategory,
} from '@/modules/core/enum/tournament/tournamentCategory';

interface CategoryChipProps {
  category: TournamentCategory;
  size?: 'small' | 'medium';
}

/**
 * A small shared chip tinted by tournament category (masculine -> orange,
 * feminine -> purple), so every surface labels a category the same way and with
 * the same brand hue instead of a generic palette color.
 */
export default function CategoryChip({
  category,
  size = 'small',
}: CategoryChipProps) {
  const { fill, ink } = categoryColor(category);

  return (
    <Chip
      size={size}
      label={TOURNAMENT_CATEGORY_LABELS[category]}
      sx={{ bgcolor: fill, color: ink, fontWeight: 600 }}
    />
  );
}
