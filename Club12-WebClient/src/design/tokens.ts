/**
 * Club 12 design tokens — the single source of truth for the brand's visual
 * language (dark-first, orange accent, navy "scoreboard" chrome). These values
 * were previously scattered as private constants inside `theme.ts`; centralizing
 * them here lets components and one-off surfaces read the same hues, radii and
 * spacing the MUI theme is built from, instead of hardcoding hex strings.
 *
 * The MUI theme (`theme.ts`) is assembled from these tokens; prefer reading
 * `theme.palette` inside components. Reach for a raw token only when you need a
 * value the theme does not expose (e.g. a specific surface layer for a custom
 * gradient or the jersey/hero accents).
 */

/** Brand hues. Orange is the single accent; navy is the secondary chrome hue. */
export const brand = {
  orange: '#FF5A1F',
  orangeLight: '#FF8A50',
  orangeDark: '#C43E00',
  /** Near-black ink for labels on filled orange (AA-safe, ~5.6:1). */
  orangeInk: '#0B0F17',
  navy: '#0F172A',
  navyLight: '#1E293B',
  /** The club's championship gold — used for champions, podium and finals. */
  gold: '#E6A817',
  goldLight: '#F5C542',
} as const;

/**
 * Playoff qualification tier colors (HU-45), used to highlight the standings
 * rows that qualify to each cup. Gold-silver-bronze for the top three cups,
 * then the brand orange for any further cup below the podium three. Silver and
 * bronze are muted metallics tuned to stay legible on the dark canvas.
 */
export const cupTier = {
  gold: brand.gold,
  silver: '#C7CDD6',
  bronze: '#CD8E5A',
  accent: brand.orange,
} as const;

/**
 * Category accent colors taken from the club's own branding: masculine pieces
 * are the warm orange, feminine pieces a vivid purple/magenta. Used to tint
 * category chips and the masculine/feminine sections so a visitor tells them
 * apart at a glance.
 */
export const category = {
  masculine: brand.orange,
  feminine: '#A32CC4',
} as const;

/**
 * Layered dark surfaces (canvas -> paper -> raised). A deliberate three-step
 * scale so depth reads through elevation, never through a colored overlay.
 */
export const surface = {
  canvas: '#111827', // L0 app canvas
  paper: '#1A2232', // L1 cards, drawers, app surfaces
  raised: '#232D3F', // L2 inputs, menus, hovered rows
} as const;

/** Light-mode surfaces, retained for the legacy light branch of the theme. */
export const surfaceLight = {
  canvas: '#F4F6F9',
  paper: '#FFFFFF',
  raised: '#FFFFFF',
} as const;

export const ink = {
  primary: '#E7EAF0',
  secondary: '#98A2B3',
  primaryLight: brand.navy,
  secondaryLight: '#516072',
} as const;

/** Semantic hues tuned to stay legible on the dark canvas. */
export const semantic = {
  success: '#00C853',
  warning: '#F5A524',
  info: '#38BDF8',
  error: '#d32f2f',
} as const;

export const dividerColor = {
  dark: 'rgba(231, 234, 240, 0.12)',
  light: 'rgba(15, 23, 42, 0.12)',
} as const;

/**
 * The Club 12 logo asset bakes its dark maroon backdrop into the PNG (no alpha),
 * so surfaces wrapping the logo use this matching color to read as a badge.
 */
export const logoBackground = '#4D0000';

/** The SweetAlert cancel affordance color, reused for destructive controls. */
export const cancelColor = '#d33';

/** Corner radii, in px, as a small deliberate scale. */
export const radius = {
  sm: 6,
  md: 8,
  lg: 10,
  xl: 16,
  pill: 999,
} as const;

/** Base spacing unit (px). The MUI `spacing()` factor stays at the default 8. */
export const spacingUnit = 8;

/** Typeface roles. Oswald (condensed, uppercase) carries the sporting display
 *  voice; Roboto handles body copy and data. */
export const font = {
  display: "'Oswald', sans-serif",
  body: "'Roboto', sans-serif",
} as const;

/**
 * Constant reserved height for a page's main content area, so a view is the
 * same height while its data loads (skeleton) and once it arrives — no layout
 * jump. Sized to fill the viewport below the public header and above the
 * footer without forcing a scroll on an empty page.
 */
export const pageMinHeight = 'calc(100vh - 220px)';
