import { brand } from './tokens';

/**
 * Team shirt colors are chosen with a color picker, so the stored value is a
 * `#rrggbb` hex. This module resolves that hex into a usable fill plus the ink
 * color that stays legible on top of it. Anything that is not a valid hex
 * (e.g. an empty value, or legacy free-text left over from before the picker)
 * falls back to the navy chrome hue so a jersey never renders with no fill.
 */

const HEX_RE = /^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/;

/** Expands a 3-digit hex (#abc) to its 6-digit form (#aabbcc). */
const expandHex = (hex: string): string => {
  if (hex.length === 4) {
    const [, r, g, b] = hex;
    return `#${r}${r}${g}${g}${b}${b}`;
  }
  return hex;
};

/** True when the value is a valid `#rgb` or `#rrggbb` hex string. */
export const isHexColor = (value?: string | null): boolean =>
  typeof value === 'string' && HEX_RE.test(value.trim());

/**
 * Turns a `#rgb`/`#rrggbb` hex into an `rgba()` string at the given alpha, so a
 * brand hue can be used as a translucent tint/overlay. Non-hex values fall back
 * to the navy chrome hue so a surface never renders with a broken color.
 */
export const hexToRgba = (hex: string, alpha: number): string => {
  const full = HEX_RE.test(hex.trim())
    ? expandHex(hex.trim())
    : brand.navyLight;
  const r = parseInt(full.slice(1, 3), 16);
  const g = parseInt(full.slice(3, 5), 16);
  const b = parseInt(full.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

/** Relative luminance (WCAG) of a #rrggbb color, in the 0..1 range. */
export const luminance = (hex: string): number => {
  const full = expandHex(hex);
  const channel = (v: number): number => {
    const s = v / 255;
    return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
  };
  const r = channel(parseInt(full.slice(1, 3), 16));
  const g = channel(parseInt(full.slice(3, 5), 16));
  const b = channel(parseInt(full.slice(5, 7), 16));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};

export interface ResolvedColor {
  /** The resolved #rrggbb fill. */
  fill: string;
  /** A contrasting ink (near-black on light fills, off-white on dark ones). */
  ink: string;
  /** True when the fill is light enough that dark ink reads better on it. */
  isLight: boolean;
}

/**
 * Turns a stored shirt-color value (a hex, or nothing) into a resolved fill
 * plus the ink color that stays legible on top of it.
 */
export const resolveShirtColor = (value?: string | null): ResolvedColor => {
  const raw = (value ?? '').trim().toLowerCase();
  const fill = HEX_RE.test(raw) ? expandHex(raw) : brand.navyLight;
  const isLight = luminance(fill) > 0.55;
  return { fill, ink: isLight ? '#0b0f17' : '#f5f5f5', isLight };
};
