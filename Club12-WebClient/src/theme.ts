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

const NAVY = '#0F172A';
const NAVY_LIGHT = '#1E293B';
const ORANGE = '#FF5A1F';
const ORANGE_DARK = '#C43E00';
const LIVE_GREEN = '#00C853';

const DARK_SURFACE = '#111827';
const DARK_SURFACE_RAISED = '#1A2232';
const DARK_TEXT_PRIMARY = '#E7EAF0';
const DARK_TEXT_SECONDARY = '#98A2B3';

/**
 * Builds the MUI theme for the requested color mode. Light and dark share
 * the same brand hues (orange primary, navy secondary) but derive
 * surface/text tokens independently so components never need to branch on
 * mode themselves — they read from `theme.palette`.
 */
export const getTheme = (mode: PaletteMode): Theme => {
  const isDark = mode === 'dark';

  const baseTheme = createTheme({
    palette: {
      mode,
      primary: {
        main: ORANGE,
        light: '#FF8A50',
        dark: ORANGE_DARK,
        contrastText: '#fff',
      },
      secondary: {
        main: isDark ? NAVY_LIGHT : NAVY,
        light: NAVY_LIGHT,
        contrastText: '#fff',
      },
      success: {
        main: LIVE_GREEN,
      },
      background: {
        default: isDark ? DARK_SURFACE : '#F4F6F9',
        paper: isDark ? DARK_SURFACE_RAISED : '#FFFFFF',
      },
      text: {
        primary: isDark ? DARK_TEXT_PRIMARY : NAVY,
        secondary: isDark ? DARK_TEXT_SECONDARY : '#516072',
      },
      divider: isDark ? 'rgba(231, 234, 240, 0.12)' : 'rgba(15, 23, 42, 0.12)',
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
            backgroundColor: isDark ? DARK_SURFACE : NAVY,
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
              backgroundColor: isDark ? DARK_SURFACE : NAVY,
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
          },
          contained: {
            color: '#fff',
          },
        },
      },
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundColor: isDark ? DARK_SURFACE : '#F4F6F9',
          },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backgroundColor: NAVY,
            color: '#fff',
          },
        },
      },
      MuiTextField: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? DARK_SURFACE_RAISED : '#fff',
            borderRadius: '8px',
          },
        },
      },
      MuiInputBase: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? DARK_SURFACE_RAISED : '#fff',
            borderRadius: '8px',
          },
        },
      },
      MuiDataGrid: {
        styleOverrides: {
          columnHeader: {
            backgroundColor: isDark ? DARK_SURFACE : NAVY,
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
          },
        },
      },
      MuiListItemButton: {
        styleOverrides: {
          root: {
            '&.Mui-selected': {
              backgroundColor: ORANGE,
              color: '#fff',
              '&:hover': {
                backgroundColor: ORANGE_DARK,
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
              color: '#fff',
              '&:hover': {
                backgroundColor: ORANGE_DARK,
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
