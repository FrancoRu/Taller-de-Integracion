import { useId } from 'react';
import { brand } from '@/design/tokens';
import { resolveShirtColor } from '@/design/colorName';
import { JerseyStyle } from '@/design/jerseyStyles';

/**
 * The basketball tank silhouette. Straps over the shoulders, a shallow scooped
 * neck, and a body that flares from the underarm down to a straight hem — the
 * recognizable "kit" shape. Patterns are clipped to this outline so every
 * template shares one silhouette. Authored on a ~230×350 canvas; the viewBox
 * frames it with a little padding. Kept as a module constant so the path is
 * defined once regardless of how many jerseys render.
 */
const BODY_PATH =
  'M148 53 L183 51 C193 58 207 61 225 61 C243 61 257 58 267 51 L300 58 ' +
  'C298 84 299 111 304 132 C309 157 321 184 339 202 L339 400 L109 400 ' +
  'L109 211 C126 196 138 174 145 151 C153 126 153 86 148 53 Z';

/** The V-neck collar, used by the `vneck` template. */
const VNECK_PATH = 'M195 52 L253 52 L224 128 Z';

/** The diagonal sash band, used by the `sash` template (clipped to the body). */
const SASH_POINTS = '96,236 214,86 352,250 236,398';

/** Chevron (downward "V") band top edges, used by the `chevron` template. */
const CHEVRON_YS = [150, 232, 314];

/** Thin vertical pinstripe x-positions, used by the `pinstripe` template. */
const PINSTRIPE_XS = [112, 136, 160, 184, 208, 232, 256, 280, 304, 328];

/** Single large chest "V" accent, used by the `arrow` template. */
const ARROW_POINTS = '110,168 224,258 338,168 338,204 224,294 110,204';

/** Diagonal color-block corner, used by the `colorblock` template. */
const COLORBLOCK_POINTS = '348,44 348,406 150,406';

/** A centered 5-point star, used by the `star` template. */
const STAR_POINTS =
  '224,155 242,206 295,207 253,239 268,291 224,260 180,291 196,239 153,207 206,206';

export interface JerseySvgProps {
  /** The team's primary shirt color, as a `#rrggbb` hex. */
  color?: string | null;
  /** The team's secondary color for pattern and trim, as a `#rrggbb` hex.
   *  When absent, a contrasting default is derived from the primary. */
  secondaryColor?: string | null;
  /** The team's third color, as a `#rrggbb` hex — only used by the
   * tri-color templates (see `JERSEY_STYLES`' `usesTertiary`) as a second
   * accent alongside `secondaryColor`. When absent, those templates fall
   * back to reusing the secondary color rather than inventing an
   * unrequested third hue. */
  tertiaryColor?: string | null;
  /** Which kit template to render. Unknown values fall back to `solid`. */
  style?: JerseyStyle | string | null;
  /** Optional dorsal to print on the chest. */
  number?: number | null;
  /** Rendered width in px (height scales to keep the aspect ratio). */
  size?: number;
  /** Accessible label; defaults to a description of the kit. */
  title?: string;
}

/**
 * Renders a team's kit as an inline, tintable SVG jersey. The primary color
 * fills the body, the secondary color draws the chosen pattern and the neck /
 * armhole trim, an optional third color adds a second accent for the
 * tri-color templates (`triband`, `splitTri`, `frame`, `ring`), and an
 * optional dorsal number is printed in a contrasting ink. Purely
 * presentational and dependency-free, so it is safe to render many at once
 * in rosters and lists.
 */
export default function JerseySvg({
  color,
  secondaryColor,
  tertiaryColor,
  style,
  number,
  size = 48,
  title,
}: JerseySvgProps) {
  const base = useId();
  const clipId = `${base}-clip`;
  const stripePatId = `${base}-stripe`;
  const dotPatId = `${base}-dot`;
  const gradId = `${base}-grad`;
  const checkerPatId = `${base}-checker`;
  const diamondPatId = `${base}-diamond`;

  const primary = resolveShirtColor(color);
  // Derive a legible secondary when none is set: white on dark kits, navy on
  // light ones, so the pattern always reads against the body.
  const secondary = secondaryColor
    ? resolveShirtColor(secondaryColor)
    : resolveShirtColor(primary.isLight ? brand.navy : '#ffffff');
  // The tri-color templates fall back to the secondary when no third color
  // was chosen — a team that never picked one still gets a coherent kit
  // instead of an unrequested extra hue appearing on its own.
  const tertiary = tertiaryColor ? resolveShirtColor(tertiaryColor) : secondary;

  const outline = primary.isLight ? 'rgba(11,15,23,0.35)' : 'rgba(245,245,245,0.4)';
  const label = title ?? 'Camiseta del equipo';
  const bodyFill = style === 'gradient' ? `url(#${gradId})` : primary.fill;

  return (
    <svg
      role="img"
      aria-label={label}
      width={size}
      height={size * 1.49}
      viewBox="100 44 248 362"
      xmlns="http://www.w3.org/2000/svg"
    >
      <title>{label}</title>
      <defs>
        <clipPath id={clipId}>
          <path d={BODY_PATH} />
        </clipPath>
        {style === 'diagonal' && (
          <pattern
            id={stripePatId}
            width="52"
            height="52"
            patternUnits="userSpaceOnUse"
            patternTransform="rotate(45)"
          >
            <rect width="52" height="52" fill={primary.fill} />
            <rect width="26" height="52" fill={secondary.fill} />
          </pattern>
        )}
        {style === 'circles' && (
          <pattern id={dotPatId} width="58" height="58" patternUnits="userSpaceOnUse">
            <circle cx="29" cy="29" r="14" fill={secondary.fill} />
          </pattern>
        )}
        {style === 'gradient' && (
          <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={primary.fill} />
            <stop offset="100%" stopColor={secondary.fill} />
          </linearGradient>
        )}
        {style === 'checkerboard' && (
          <pattern id={checkerPatId} width="44" height="44" patternUnits="userSpaceOnUse">
            <rect width="44" height="44" fill={primary.fill} />
            <rect width="22" height="22" fill={secondary.fill} />
            <rect x="22" y="22" width="22" height="22" fill={secondary.fill} />
          </pattern>
        )}
        {style === 'diamonds' && (
          <pattern id={diamondPatId} width="52" height="52" patternUnits="userSpaceOnUse">
            <rect
              x="10"
              y="10"
              width="32"
              height="32"
              fill={secondary.fill}
              transform="rotate(45 26 26)"
            />
          </pattern>
        )}
      </defs>

      {/* Body fill + pattern, all clipped to the jersey outline. */}
      <g clipPath={`url(#${clipId})`}>
        <rect x="100" y="44" width="248" height="362" fill={bodyFill} />

        {style === 'stripes' &&
          [118, 163, 208, 253, 298].map(x => (
            <rect key={x} x={x} y="44" width="22" height="362" fill={secondary.fill} />
          ))}

        {style === 'hoops' &&
          [80, 140, 200, 260, 320].map(y => (
            <rect key={y} x="100" y={y} width="248" height="28" fill={secondary.fill} />
          ))}

        {style === 'diagonal' && (
          <rect x="100" y="44" width="248" height="362" fill={`url(#${stripePatId})`} />
        )}

        {style === 'circles' && (
          <rect x="100" y="44" width="248" height="362" fill={`url(#${dotPatId})`} />
        )}

        {style === 'chevron' &&
          CHEVRON_YS.map(y => (
            <polygon
              key={y}
              points={`120,${y} 224,${y + 50} 328,${y} 328,${y + 26} 224,${y + 76} 120,${y + 26}`}
              fill={secondary.fill}
            />
          ))}

        {style === 'sides' && (
          <>
            <rect x="100" y="44" width="46" height="362" fill={secondary.fill} />
            <rect x="302" y="44" width="46" height="362" fill={secondary.fill} />
          </>
        )}

        {style === 'halves' && (
          <rect x="224" y="44" width="124" height="362" fill={secondary.fill} />
        )}

        {style === 'sash' && <polygon points={SASH_POINTS} fill={secondary.fill} />}

        {style === 'vneck' && <path d={VNECK_PATH} fill={secondary.fill} />}

        {style === 'pinstripe' &&
          PINSTRIPE_XS.map(x => (
            <rect key={x} x={x} y="44" width="4" height="362" fill={secondary.fill} />
          ))}

        {style === 'yoke' && (
          <rect x="100" y="44" width="248" height="68" fill={secondary.fill} />
        )}

        {style === 'colorblock' && (
          <polygon points={COLORBLOCK_POINTS} fill={secondary.fill} />
        )}

        {style === 'arrow' && <polygon points={ARROW_POINTS} fill={secondary.fill} />}

        {style === 'camo' && (
          <>
            <ellipse cx="150" cy="120" rx="40" ry="26" fill={secondary.fill} transform="rotate(-15 150 120)" />
            <ellipse cx="230" cy="90" rx="34" ry="22" fill={secondary.fill} transform="rotate(20 230 90)" />
            <ellipse cx="300" cy="160" rx="38" ry="24" fill={secondary.fill} transform="rotate(-10 300 160)" />
            <ellipse cx="160" cy="260" rx="42" ry="28" fill={secondary.fill} transform="rotate(12 160 260)" />
            <ellipse cx="280" cy="300" rx="36" ry="24" fill={secondary.fill} transform="rotate(-25 280 300)" />
            <ellipse cx="220" cy="360" rx="44" ry="26" fill={secondary.fill} transform="rotate(8 220 360)" />
          </>
        )}

        {style === 'checkerboard' && (
          <rect x="100" y="44" width="248" height="362" fill={`url(#${checkerPatId})`} />
        )}

        {style === 'diamonds' && (
          <rect x="100" y="44" width="248" height="362" fill={`url(#${diamondPatId})`} />
        )}

        {style === 'star' && <polygon points={STAR_POINTS} fill={secondary.fill} />}

        {style === 'triband' && (
          <>
            <rect x="100" y="160" width="248" height="70" fill={secondary.fill} />
            <rect x="100" y="290" width="248" height="70" fill={tertiary.fill} />
          </>
        )}

        {style === 'shoulder' && (
          <>
            <rect x="100" y="44" width="70" height="50" fill={secondary.fill} />
            <rect x="278" y="44" width="70" height="50" fill={secondary.fill} />
          </>
        )}

        {style === 'splitTri' && (
          <>
            <rect x="182" y="44" width="83" height="362" fill={secondary.fill} />
            <rect x="265" y="44" width="83" height="362" fill={tertiary.fill} />
          </>
        )}

        {style === 'frame' && (
          <path d={BODY_PATH} fill="none" stroke={tertiary.fill} strokeWidth="24" />
        )}

        {style === 'crossband' && (
          <>
            <rect
              x="120"
              y="205"
              width="208"
              height="34"
              fill={secondary.fill}
              transform="rotate(45 224 222)"
            />
            <rect
              x="120"
              y="205"
              width="208"
              height="34"
              fill={secondary.fill}
              transform="rotate(-45 224 222)"
            />
          </>
        )}

        {style === 'ring' && (
          <circle cx="224" cy="215" r="70" fill="none" stroke={tertiary.fill} strokeWidth="14" />
        )}
      </g>

      {/* Neck + hem trim traces the secondary color along the outline. */}
      <path
        d={BODY_PATH}
        fill="none"
        stroke={secondary.fill}
        strokeWidth="8"
        strokeLinejoin="round"
      />
      {/* Thin definition edge so the kit reads on any background. */}
      <path
        d={BODY_PATH}
        fill="none"
        stroke={outline}
        strokeWidth="2"
        strokeLinejoin="round"
      />

      {number != null && (
        <text
          x="224"
          y="215"
          textAnchor="middle"
          dominantBaseline="middle"
          fontFamily="'Oswald', sans-serif"
          fontWeight="700"
          fontSize="96"
          fill={primary.ink}
          // A contrasting halo (the opposite of the ink) drawn behind the
          // glyphs keeps the dorsal legible even where it crosses a pattern
          // in the secondary color (stripes, sash, halves, ...). Every
          // caller that passes `number` renders this at 28-30px (roster and
          // scorer lists) — the viewBox is 248 units wide, so that's an
          // ~8x downscale. At the old strokeWidth (6) the halo shrank to a
          // sub-pixel fraction of a px and stopped doing anything, which is
          // exactly why the number read poorly on so many jerseys — it was
          // riding on raw ink-vs-fill contrast alone. Wide enough here to
          // stay a solid ~2px ring at those real render sizes.
          stroke={primary.isLight ? '#f5f5f5' : '#0b0f17'}
          strokeWidth="16"
          strokeLinejoin="round"
          paintOrder="stroke"
        >
          {number}
        </text>
      )}
    </svg>
  );
}
