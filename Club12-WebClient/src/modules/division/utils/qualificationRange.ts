import { QualificationRange } from '@/modules/division/type/division.d';
import { cupTier } from '@/design/tokens';

/**
 * Finds the qualification range whose [fromPosition, toPosition] span contains
 * the given 1-based standings position, or `undefined` when no range covers it
 * (that row does not qualify to any cup). Ranges never overlap (the backend
 * enforces it), so at most one can match.
 */
export const findQualificationRange = (
  ranges: QualificationRange[] | undefined,
  position: number
): QualificationRange | undefined =>
  ranges?.find(range => position >= range.fromPosition && position <= range.toPosition);

/**
 * The tier color a cup is painted with, by its top-down order: 0 gold, 1
 * silver, 2 bronze, and the brand-orange accent for any further cup. Reads the
 * centralized design tokens so the standings highlight and legend never
 * hardcode a hex.
 */
export const cupTierColor = (order: number): string => {
  switch (order) {
    case 0:
      return cupTier.gold;
    case 1:
      return cupTier.silver;
    case 2:
      return cupTier.bronze;
    default:
      return cupTier.accent;
  }
};

/**
 * A small emoji marker per tier, so the legend conveys the cup rank without
 * relying on color alone (the cup name text carries the full meaning).
 */
export const cupTierMarker = (order: number): string => {
  switch (order) {
    case 0:
      return '🟡';
    case 1:
      return '⚪';
    case 2:
      return '🟠';
    default:
      return '🔶';
  }
};
