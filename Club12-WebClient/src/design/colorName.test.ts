import { describe, expect, it } from 'vitest';
import { isHexColor, luminance, resolveShirtColor } from './colorName';
import { brand } from './tokens';

describe('isHexColor', () => {
  it('accepts #rgb and #rrggbb', () => {
    expect(isHexColor('#f00')).toBe(true);
    expect(isHexColor('#FF0000')).toBe(true);
  });

  it('rejects names, empty, and malformed values', () => {
    expect(isHexColor('rojo')).toBe(false);
    expect(isHexColor('')).toBe(false);
    expect(isHexColor(undefined)).toBe(false);
    expect(isHexColor('#12')).toBe(false);
  });
});

describe('luminance', () => {
  it('is near 0 for black and near 1 for white', () => {
    expect(luminance('#000000')).toBeCloseTo(0, 2);
    expect(luminance('#ffffff')).toBeCloseTo(1, 2);
  });
});

describe('resolveShirtColor', () => {
  it('keeps a valid hex and expands the short form', () => {
    expect(resolveShirtColor('#FF0000').fill).toBe('#ff0000');
    expect(resolveShirtColor('#fff').fill).toBe('#ffffff');
  });

  it('picks dark ink on a light fill and light ink on a dark fill', () => {
    const white = resolveShirtColor('#ffffff');
    expect(white.isLight).toBe(true);
    expect(white.ink).toBe('#0b0f17');

    const navy = resolveShirtColor('#0f2e6b');
    expect(navy.isLight).toBe(false);
    expect(navy.ink).toBe('#f5f5f5');
  });

  it('falls back to the navy chrome hue for non-hex or empty values', () => {
    expect(resolveShirtColor('Rojo').fill).toBe(brand.navyLight);
    expect(resolveShirtColor(undefined).fill).toBe(brand.navyLight);
    expect(resolveShirtColor('').fill).toBe(brand.navyLight);
  });
});
