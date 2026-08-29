import { describe, expect, it } from 'vitest';
import { QualificationRange } from '@/modules/division/type/division.d';
import {
  cupTierColor,
  cupTierMarker,
  findQualificationRange,
} from './qualificationRange';
import { cupTier } from '@/design/tokens';

const ranges: QualificationRange[] = [
  { fromPosition: 1, toPosition: 4, cupName: 'Copa Oro', order: 0 },
  { fromPosition: 5, toPosition: 8, cupName: 'Copa Plata', order: 1 },
];

describe('findQualificationRange', () => {
  it('matches the first position of a range (inclusive lower bound)', () => {
    expect(findQualificationRange(ranges, 1)?.cupName).toBe('Copa Oro');
  });

  it('matches the last position of a range (inclusive upper bound)', () => {
    expect(findQualificationRange(ranges, 8)?.cupName).toBe('Copa Plata');
  });

  it('matches a position inside a range', () => {
    expect(findQualificationRange(ranges, 6)?.cupName).toBe('Copa Plata');
  });

  it('returns undefined for a position outside every range', () => {
    expect(findQualificationRange(ranges, 9)).toBeUndefined();
  });

  it('returns undefined when there are no ranges', () => {
    expect(findQualificationRange(undefined, 1)).toBeUndefined();
    expect(findQualificationRange([], 1)).toBeUndefined();
  });
});

describe('cupTierColor', () => {
  it('maps order to gold, silver, bronze then the accent', () => {
    expect(cupTierColor(0)).toBe(cupTier.gold);
    expect(cupTierColor(1)).toBe(cupTier.silver);
    expect(cupTierColor(2)).toBe(cupTier.bronze);
    expect(cupTierColor(3)).toBe(cupTier.accent);
    expect(cupTierColor(7)).toBe(cupTier.accent);
  });
});

describe('cupTierMarker', () => {
  it('returns a distinct marker per tier so the legend does not rely on color alone', () => {
    expect(cupTierMarker(0)).toBe('🟡');
    expect(cupTierMarker(1)).toBe('⚪');
    expect(cupTierMarker(2)).toBe('🟠');
    expect(cupTierMarker(4)).toBe('🔶');
  });
});
