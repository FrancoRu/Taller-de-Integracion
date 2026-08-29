import { describe, expect, it } from 'vitest';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { categoryColor } from './categoryColor';
import { category } from './tokens';

describe('categoryColor', () => {
  it('tints the masculine category with the brand orange', () => {
    const { fill, ink } = categoryColor(TournamentCategory.Masculine);

    expect(fill).toBe(category.masculine);
    // The orange is dark enough that the off-white ink reads on it.
    expect(ink).toBe('#f5f5f5');
  });

  it('tints the feminine category with the brand purple', () => {
    const { fill, ink } = categoryColor(TournamentCategory.Feminine);

    expect(fill).toBe('#A32CC4');
    expect(ink).toBe('#f5f5f5');
  });
});
