import { useId } from 'react';
import { brand } from '@/design/tokens';
import { resolveShirtColor } from '@/design/colorName';
import { JerseyStyle } from '@/design/jerseyStyles';

/**
 * The basketball tank silhouette, on a 100x124 canvas. Straps over the
 * shoulders, a scooped neck, wide armholes, tapering to the hem. Patterns are
 * clipped to this exact outline so every template shares one recognizable
 * shape (the "kit" convention). Kept as a module constant so the path is
 * defined once regardless of how many jerseys render.
 */
const BODY_PATH =
  'M30,22 C34,18 40,17 44,18 L46,21 C48,27 52,27 54,21 L56,18 ' +
  'C60,17 66,18 70,22 L82,30 C85,32 85,35 82,41 L70,51 ' +
  'C68,53 68,55 68,59 L72,108 C72,111 70,112 67,112 L33,112 ' +
  'C30,112 28,111 28,108 L32,59 C32,55 32,53 30,51 L18,41 ' +
  'C15,35 15,32 18,30 Z';

/** The V-neck collar triangle, used by the `vneck` template. */
const VNECK_PATH = 'M42,20 L58,20 L50,44 Z';

/** The diagonal sash band, used by the `sash` template (clipped to the body). */
const SASH_POINTS = '12,60 46,28 90,72 56,104';

/** Chevron (downward "V") bands, used by the `chevron` template. */
const CHEVRON_YS = [40, 62, 84];

export interface JerseySvgProps {
  /** The team's primary shirt color, as a `#rrggbb` hex. */
  color?: string | null;
  /** The team's secondary color for pattern and trim, as a `#rrggbb` hex.
   *  When absent, a contrasting default is derived from the primary. */
  secondaryColor?: string | null;
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
 * armhole trim, and an optional dorsal number is printed in a contrasting ink.
 * Purely presentational and dependency-free, so it is safe to render many at
 * once in rosters and lists.
 */
export default function JerseySvg({
  color,
  secondaryColor,
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

  const primary = resolveShirtColor(color);
  // Derive a legible secondary when none is set: white on dark kits, navy on
  // light ones, so the pattern always reads against the body.
  const secondary = secondaryColor
    ? resolveShirtColor(secondaryColor)
    : resolveShirtColor(primary.isLight ? brand.navy : '#ffffff');

  const outline = primary.isLight ? 'rgba(11,15,23,0.35)' : 'rgba(245,245,245,0.4)';
  const label = title ?? 'Camiseta del equipo';
  const bodyFill = style === 'gradient' ? `url(#${gradId})` : primary.fill;

  return (
    <svg
      role="img"
      aria-label={label}
      width={size}
      height={size * 1.24}
      viewBox="0 0 100 124"
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
            width="18"
            height="18"
            patternUnits="userSpaceOnUse"
            patternTransform="rotate(45)"
          >
            <rect width="18" height="18" fill={primary.fill} />
            <rect width="9" height="18" fill={secondary.fill} />
          </pattern>
        )}
        {style === 'circles' && (
          <pattern id={dotPatId} width="22" height="22" patternUnits="userSpaceOnUse">
            <circle cx="11" cy="11" r="5" fill={secondary.fill} />
          </pattern>
        )}
        {style === 'gradient' && (
          <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={primary.fill} />
            <stop offset="100%" stopColor={secondary.fill} />
          </linearGradient>
        )}
      </defs>

      {/* Body fill + pattern, all clipped to the jersey outline. */}
      <g clipPath={`url(#${clipId})`}>
        <rect x="0" y="0" width="100" height="124" fill={bodyFill} />

        {style === 'stripes' &&
          [18, 32, 46, 60, 74].map(x => (
            <rect key={x} x={x} y="0" width="7" height="124" fill={secondary.fill} />
          ))}

        {style === 'hoops' &&
          [24, 44, 64, 84, 104].map(y => (
            <rect key={y} x="0" y={y} width="100" height="10" fill={secondary.fill} />
          ))}

        {style === 'diagonal' && (
          <rect x="0" y="0" width="100" height="124" fill={`url(#${stripePatId})`} />
        )}

        {style === 'circles' && (
          <rect x="0" y="0" width="100" height="124" fill={`url(#${dotPatId})`} />
        )}

        {style === 'chevron' &&
          CHEVRON_YS.map(y => (
            <polygon
              key={y}
              points={`14,${y} 50,${y + 16} 86,${y} 86,${y + 8} 50,${y + 24} 14,${y + 8}`}
              fill={secondary.fill}
            />
          ))}

        {style === 'sides' && (
          <>
            <rect x="14" y="0" width="14" height="124" fill={secondary.fill} />
            <rect x="72" y="0" width="14" height="124" fill={secondary.fill} />
          </>
        )}

        {style === 'halves' && (
          <rect x="50" y="0" width="50" height="124" fill={secondary.fill} />
        )}

        {style === 'sash' && <polygon points={SASH_POINTS} fill={secondary.fill} />}

        {style === 'vneck' && <path d={VNECK_PATH} fill={secondary.fill} />}
      </g>

      {/* Neck + armhole trim traces the secondary color along the outline. */}
      <path
        d={BODY_PATH}
        fill="none"
        stroke={secondary.fill}
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      {/* Thin definition edge so the kit reads on any background. */}
      <path
        d={BODY_PATH}
        fill="none"
        stroke={outline}
        strokeWidth="0.75"
        strokeLinejoin="round"
      />

      {number != null && (
        <text
          x="50"
          y="82"
          textAnchor="middle"
          dominantBaseline="middle"
          fontFamily="'Oswald', sans-serif"
          fontWeight="700"
          fontSize="34"
          fill={primary.ink}
        >
          {number}
        </text>
      )}
    </svg>
  );
}
