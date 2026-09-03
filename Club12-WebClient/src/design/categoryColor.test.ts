import { describe, expect, it } from 'vitest';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { categoryColor } from './categoryColor';
import { category } from './tokens';

describe('categoryColor', () => {
  it('tints the masculine category with the brand orange', () => {
    const { fill, ink } = categoryColor(TournamentCategory.Masculine);

    expect(fill).toBe(category.masculine);
    // Regression: white text on this orange is only ~2.86:1 (fails WCAG AA's
    // 4.5:1) — dark ink is the one that actually reads on it, ~6.3:1.
    expect(ink).toBe('#0b0f17');
  });

  it('tints the feminine category with the brand purple', () => {
    const { fill, ink } = categoryColor(TournamentCategory.Feminine);

    expect(fill).toBe('#A32CC4');
    expect(ink).toBe('#f5f5f5');
  });
});
