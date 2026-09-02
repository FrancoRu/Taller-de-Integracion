import { describe, expect, it } from 'vitest';
import {
  DEFAULT_JERSEY_STYLE,
  JERSEY_STYLES,
  isJerseyStyle,
  toJerseyStyle,
} from './jerseyStyles';

describe('jerseyStyles', () => {
  it('exposes a labelled option per template', () => {
    expect(JERSEY_STYLES.map(s => s.value)).toEqual([
      'solid',
      'stripes',
      'hoops',
      'diagonal',
      'chevron',
      'sash',
      'sides',
      'halves',
      'circles',
      'gradient',
      'vneck',
      'pinstripe',
      'yoke',
      'colorblock',
      'arrow',
    ]);
    JERSEY_STYLES.forEach(s => expect(s.label.trim().length).toBeGreaterThan(0));
  });

  it('narrows only known styles', () => {
    expect(isJerseyStyle('stripes')).toBe(true);
    expect(isJerseyStyle('bogus')).toBe(false);
    expect(isJerseyStyle(undefined)).toBe(false);
  });

  it('coerces unknown values to the default style', () => {
    expect(toJerseyStyle('sash')).toBe('sash');
    expect(toJerseyStyle('bogus')).toBe(DEFAULT_JERSEY_STYLE);
    expect(toJerseyStyle(null)).toBe(DEFAULT_JERSEY_STYLE);
  });
});
