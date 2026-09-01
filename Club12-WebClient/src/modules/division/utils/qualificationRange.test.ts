import { describe, expect, it } from 'vitest';
import { QualificationRange } from '@/modules/division/type/division.d';
import {
  buildCrossCupGroupQualificationRange,
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

describe('buildCrossCupGroupQualificationRange', () => {
  it('builds a single range covering positions 1..qualifiersPerGroup, named after the cup', () => {
    const result = buildCrossCupGroupQualificationRange({
      name: 'Copa Club12',
      qualifiersPerGroup: 2,
    });

    expect(result).toEqual([
      { fromPosition: 1, toPosition: 2, cupName: 'Copa Club12', order: 0 },
    ]);
  });

  it('returns undefined when qualifiersPerGroup is missing or non-positive', () => {
    expect(
      buildCrossCupGroupQualificationRange({ name: 'Zona A', qualifiersPerGroup: undefined })
    ).toBeUndefined();
    expect(
      buildCrossCupGroupQualificationRange({ name: 'Zona A', qualifiersPerGroup: 0 })
    ).toBeUndefined();
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
