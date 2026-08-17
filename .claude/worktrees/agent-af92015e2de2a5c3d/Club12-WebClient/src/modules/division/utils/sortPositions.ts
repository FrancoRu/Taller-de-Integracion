import { Position } from '@/modules/division/type/division.d';

/**
 * Sorts division standings rows by points, then point difference, then
 * points scored — the same ranking rule used across the standings table
 * and the printable results sheet.
 */
export const sortPositions = (positions: Position[]): Position[] =>
  [...positions].sort((a, b) => {
    if (b.points !== a.points) {
      return b.points - a.points;
    }
    if (b.pointsDifference !== a.pointsDifference) {
      return b.pointsDifference - a.pointsDifference;
    }
    return b.pointsFor - a.pointsFor;
  });
