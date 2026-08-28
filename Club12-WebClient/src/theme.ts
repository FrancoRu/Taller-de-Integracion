import { createTheme, type PaletteMode, type Theme } from '@mui/material/styles';
import { esES as dataGridEsES } from '@mui/x-data-grid/locales';

export const CANCEL_BUTTON_COLOR = '#d33';

/**
 * The Club 12 logo asset has its dark maroon backdrop baked into the PNG
 * (no alpha transparency). This is the matching color for the chip wrapping
 * the logo wherever it's placed, so the image's rectangular edge reads as
 * an intentional badge rather than a stray box.
 */
export const LOGO_BACKGROUND_COLOR = '#4D0000';

/* Brand hues. Orange is the single accent — CTAs, active states, focus rings,
   highlights. Navy is the secondary "scoreboard" hue used for chrome. */
const ORANGE = '#FF5A1F';
const ORANGE_LIGHT = '#FF8A50';
const ORANGE_DARK = '#C43E00';
const NAVY = '#0F172A';
const NAVY_LIGHT = '#1E293B';

/* Layered dark surfaces (canvas -> paper -> raised). Kept as a deliberate
   three-step scale so depth reads through elevation, never through a colored
   MUI overlay (see MuiPaper.backgroundImage: 'none' below). */
const DARK_BG = '#111827'; // L0 app canvas
const DARK_PAPER = '#1A2232'; // L1 cards, drawers, app surfaces
const DARK_RAISED = '#232D3F'; // L2 inputs, menus, hovered rows
const DARK_TEXT_PRIMARY = '#E7EAF0';
const DARK_TEXT_SECONDARY = '#98A2B3';
const DARK_DIVIDER = 'rgba(231, 234, 240, 0.12)';

/* Near-black ink used as the label color on filled orange (AA-safe:
   ~5.6:1 vs #FF5A1F, where white would only reach ~3.1:1). */
const ORANGE_INK = '#0B0F17';

/* Semantic hues tuned to stay legible on the dark canvas. error is left to
   MUI's default (#d32f2f) so it matches the SweetAlert cancel affordance. */
const SUCCESS = '#00C853';
const WARNING = '#F5A524';
const INFO = '#38BDF8';

/**
 * Builds the MUI theme for the requested color mode. The app is dark-first;
 * the light branch is retained for the legacy default export and its token
 * test. Both modes share the brand hues (orange accent, navy secondary) but
 * derive surface/text tokens independently so components never branch on mode
 * themselves — they read from `theme.palette`.
 */
export const getTheme = (mode: PaletteMode): Theme => {
  const isDark = mode === 'dark';

  const baseTheme = createTheme({
    palette: {
      mode,
      // Bias contrastText toward the darker option so filled-orange controls
      // get AA-compliant ink rather than low-contrast white.
      contrastThreshold: 4.5,
      primary: {
        main: ORANGE,
        light: ORANGE_LIGHT,
        dark: ORANGE_DARK,
        contrastText: ORANGE_INK,
      },
      secondary: {
        main: isDark ? NAVY_LIGHT : NAVY,
        light: NAVY_LIGHT,
        contrastText: '#fff',
      },
      success: {
        main: SUCCESS,
      },
      warning: {
        main: WARNING,
      },
      info: {
        main: INFO,
      },
      background: {
        default: isDark ? DARK_BG : '#F4F6F9',
        paper: isDark ? DARK_PAPER : '#FFFFFF',
      },
      text: {
        primary: isDark ? DARK_TEXT_PRIMARY : NAVY,
        secondary: isDark ? DARK_TEXT_SECONDARY : '#516072',
      },
      divider: isDark ? DARK_DIVIDER : 'rgba(15, 23, 42, 0.12)',
    },
    typography: {
      fontFamily: "'Roboto', sans-serif",
      h1: { fontFamily: "'Oswald', sans-serif", fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.03em' },
      h2: { fontFamily: "'Oswald', sans-serif", fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.03em' },
      h3: { fontFamily: "'Oswald', sans-serif", fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.02em' },
      h4: { fontFamily: "'Oswald', sans-serif", fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.02em' },
      h5: { fontFamily: "'Oswald', sans-serif", fontWeight: 600 },
      h6: { fontFamily: "'Oswald', sans-serif", fontWeight: 600 },
    },
  });

  return createTheme(baseTheme, {
    components: {
      MuiListItemText: {
        styleOverrides: {
          primary: {
            fontWeight: 'bold',
          },
        },
      },
      MuiTableHead: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? DARK_BG : NAVY,
            '& .MuiTableCell-root': {
              color: '#fff',
              fontWeight: 700,
              textTransform: 'uppercase',
              fontSize: '0.8rem',
              letterSpacing: '0.04em',
            },
          },
        },
      },
      MuiTableRow: {
        styleOverrides: {
          root: {
            '&:hover': {
              backgroundColor: isDark ? 'rgba(255, 90, 31, 0.12)' : '#FFE9DD',
              transition: 'background-color 0.2s ease-in-out',
            },
            '&.MuiTableRow-head:hover': {
              backgroundColor: isDark ? DARK_BG : NAVY,
            },
          },
        },
      },
      MuiButton: {
        defaultProps: {
          color: 'primary',
        },
        styleOverrides: {
          root: {
            borderRadius: '8px',
            textTransform: 'none',
            marginTop: '16px',
            fontWeight: 'bold',
            // Visible keyboard focus ring in the accent hue.
            '&.Mui-focusVisible': {
              outline: `2px solid ${ORANGE}`,
              outlineOffset: '2px',
            },
          },
          containedPrimary: {
            color: ORANGE_INK,
            '&:hover': {
              backgroundColor: ORANGE_DARK,
              color: '#fff',
            },
          },
        },
      },
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundColor: isDark ? DARK_BG : '#F4F6F9',
          },
          // Global accent focus ring for keyboard users across every
          // focusable element that doesn't ship its own.
          '*:focus-visible': {
            outline: `2px solid ${ORANGE}`,
            outlineOffset: '2px',
          },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backgroundColor: NAVY,
            color: '#fff',
            // Orange keel-line ties the chrome to the accent identity.
            borderBottom: `2px solid ${ORANGE}`,
          },
        },
      },
      MuiTextField: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? DARK_RAISED : '#fff',
            borderRadius: '8px',
          },
        },
      },
      MuiInputBase: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? DARK_RAISED : '#fff',
            borderRadius: '8px',
          },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          notchedOutline: {
            borderColor: isDark ? DARK_DIVIDER : 'rgba(15, 23, 42, 0.23)',
          },
          root: {
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: isDark ? 'rgba(231, 234, 240, 0.28)' : 'rgba(15, 23, 42, 0.4)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: ORANGE,
              borderWidth: '2px',
            },
          },
        },
      },
      MuiDataGrid: {
        styleOverrides: {
          root: {
            border: '1px solid',
            borderColor: isDark ? DARK_DIVIDER : 'rgba(15, 23, 42, 0.12)',
            backgroundColor: isDark ? DARK_PAPER : '#fff',
          },
          columnHeader: {
            backgroundColor: isDark ? DARK_BG : NAVY,
            color: '#fff',
          },
          columnHeaderTitle: {
            fontWeight: 700,
            fontSize: '0.85rem',
            textTransform: 'uppercase',
            letterSpacing: '0.03em',
          },
          columnSeparator: {
            color: NAVY_LIGHT,
          },
          cell: {
            fontSize: '0.95rem',
            borderColor: isDark ? DARK_DIVIDER : 'rgba(15, 23, 42, 0.08)',
          },
          row: {
            '&:hover': {
              backgroundColor: isDark ? 'rgba(255, 90, 31, 0.10)' : '#FFE9DD',
            },
            '&.Mui-selected': {
              backgroundColor: isDark ? 'rgba(255, 90, 31, 0.18)' : 'rgba(255, 90, 31, 0.14)',
              '&:hover': {
                backgroundColor: isDark ? 'rgba(255, 90, 31, 0.24)' : 'rgba(255, 90, 31, 0.2)',
              },
            },
          },
        },
      },
      MuiListItemButton: {
        styleOverrides: {
          root: {
            '&.Mui-selected': {
              backgroundColor: ORANGE,
              color: ORANGE_INK,
              '& .MuiListItemIcon-root': { color: ORANGE_INK },
              '&:hover': {
                backgroundColor: ORANGE_DARK,
                color: '#fff',
                '& .MuiListItemIcon-root': { color: '#fff' },
              },
            },
          },
        },
      },
      MuiMenuItem: {
        styleOverrides: {
          root: {
            '&.Mui-selected': {
              backgroundColor: ORANGE,
              color: ORANGE_INK,
              '&:hover': {
                backgroundColor: ORANGE_DARK,
                color: '#fff',
              },
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            padding: baseTheme.spacing(3),
            borderRadius: '10px',
            borderTop: `4px solid ${ORANGE}`,
            boxShadow: baseTheme.shadows[3],
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            fontWeight: 700,
            textTransform: 'uppercase',
            fontSize: '0.7rem',
            letterSpacing: '0.03em',
          },
        },
      },
    },
  }, dataGridEsES);
};

const theme = getTheme('light');

export default theme;
