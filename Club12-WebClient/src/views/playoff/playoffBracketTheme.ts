import type { LibraryBracketOptions, LibraryBracketTheme } from '@/modules/playoff/type/gLootBracketTypes.d';
import { getTheme } from '@/theme';

/**
 * Fixed height (px) of every match slot in the bracket, shared by every
 * round. The library absolutely-positions each match within this box, so
 * every card — a plain single game or a best-of-N series — is exactly the
 * same two-team-row shape (see `BracketMatchNode`'s `formatBadge`, an
 * absolutely-positioned corner badge that adds no layout height). That's
 * what actually let this shrink to fit two rows + padding: the previous
 * per-game chip breakdown needed real flow height, was only relevant to
 * series rounds, and forced every OTHER round's box just as tall for
 * nothing — the deepest early rounds (Octavos, Cuartos) have the most
 * matches, so that wasted height dominated the whole bracket's size and
 * kept it from fitting the viewport vertically. The per-game detail still
 * exists in full, just in `SeriesCard`'s "Partidos de playoff" list next
 * to the bracket instead of crammed into this card too.
 */
export const PLAYOFF_BRACKET_BOX_HEIGHT = 80;

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
  spaceBetweenColumns: 40,
  spaceBetweenRows: 16,
  connectorColor: darkTheme.palette.primary.main,
  connectorColorHighlight: darkTheme.palette.primary.light,
  // Round headers ("Cuartos de final", "Semifinal", "Final") were easy to
  // miss at 12px grey-on-transparent — bumped size/weight-equivalent color
  // and given a tinted panel so each phase reads clearly at a glance.
  roundHeader: {
    isShown: true,
    height: 40,
    marginBottom: 20,
    fontSize: 14,
    fontColor: darkTheme.palette.primary.main,
    backgroundColor: 'rgba(255, 90, 31, 0.1)',
    fontFamily: "'Oswald', sans-serif",
  },
};
