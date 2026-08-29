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
        >
          {number}
        </text>
      )}
    </svg>
  );
}
