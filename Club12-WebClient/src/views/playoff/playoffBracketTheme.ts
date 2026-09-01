import type { LibraryBracketOptions, LibraryBracketTheme } from '@/modules/playoff/type/gLootBracketTypes.d';
import { getTheme } from '@/theme';

/**
 * Fixed height (px) of every match slot in the bracket, shared by every
 * round and every card regardless of content (a plain match or a
 * best-of-N series card with its per-game breakdown). The library
 * absolutely-positions each match within this box, so the card content
 * must stay within it — see `BracketMatchNode`'s 2-line-clamped per-game
 * summary. Tall enough to fit a best-of-7 series' game summary wrapped
 * onto two lines instead of truncating to one.
 */
export const PLAYOFF_BRACKET_BOX_HEIGHT = 140;

const darkTheme = getTheme('dark');

/**
 * `@g-loot/react-tournament-brackets`'s theme, reskinned to this app's dark
 * "modern sport" palette (see `src/theme.ts`). The library's own match
 * card is never rendered (a custom `matchComponent` is supplied instead),
 * so most of these tokens only affect the SVG canvas and round headers;
 * they're still filled in fully so nothing falls back to the library's
 * default light theme.
 */
export const PLAYOFF_BRACKET_THEME: LibraryBracketTheme = {
  fontFamily: darkTheme.typography.fontFamily as string,
  transitionTimingFunction: 'ease-in-out',
  disabledColor: darkTheme.palette.text.disabled ?? darkTheme.palette.text.secondary,
  roundHeaders: {
    background: darkTheme.palette.background.paper,
  },
  matchBackground: {
    wonColor: darkTheme.palette.background.paper,
    lostColor: darkTheme.palette.background.paper,
  },
  border: {
    color: darkTheme.palette.divider,
    highlightedColor: darkTheme.palette.primary.main,
  },
  textColor: {
    highlighted: darkTheme.palette.text.primary,
    main: darkTheme.palette.text.primary,
    dark: darkTheme.palette.text.secondary,
    disabled: darkTheme.palette.text.secondary,
  },
  score: {
    text: {
      highlightedWonColor: darkTheme.palette.primary.main,
      highlightedLostColor: darkTheme.palette.text.secondary,
    },
    background: {
      wonColor: 'rgba(255, 90, 31, 0.12)',
      lostColor: 'transparent',
    },
  },
  canvasBackground: 'transparent',
};

/**
 * Layout and connector-line styling for the bracket's SVG canvas — the
 * pieces of the look the library doesn't yet drive from `theme` (per its
 * own docs). Connector lines use the brand orange so they read clearly
 * against the dark canvas; `Match`/round-header sizing matches
 * `PLAYOFF_BRACKET_BOX_HEIGHT`.
 */
export const PLAYOFF_BRACKET_OPTIONS: LibraryBracketOptions = {
  boxHeight: PLAYOFF_BRACKET_BOX_HEIGHT,
  spaceBetweenColumns: 48,
  spaceBetweenRows: 24,
  connectorColor: darkTheme.palette.primary.main,
  connectorColorHighlight: darkTheme.palette.primary.light,
  roundHeader: {
    isShown: true,
    height: 32,
    marginBottom: 16,
    fontSize: 12,
    fontColor: darkTheme.palette.text.secondary,
    backgroundColor: 'transparent',
    fontFamily: "'Oswald', sans-serif",
  },
};
