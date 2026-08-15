import { createTheme } from '@mui/material/styles';

export const CANCEL_BUTTON_COLOR = '#d33';

const NAVY = '#0F172A';
const NAVY_LIGHT = '#1E293B';
const ORANGE = '#FF5A1F';
const ORANGE_DARK = '#C43E00';
const LIVE_GREEN = '#00C853';

const baseTheme = createTheme({
  palette: {
    primary: {
      main: ORANGE,
      light: '#FF8A50',
      dark: ORANGE_DARK,
      contrastText: '#fff',
    },
    secondary: {
      main: NAVY,
      light: NAVY_LIGHT,
      contrastText: '#fff',
    },
    success: {
      main: LIVE_GREEN,
    },
    background: {
      default: '#F4F6F9',
      paper: '#FFFFFF',
    },
    text: {
      primary: NAVY,
      secondary: '#516072',
    },
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

const theme = createTheme(baseTheme, {
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
          backgroundColor: NAVY,
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
            backgroundColor: '#FFE9DD',
            transition: 'background-color 0.2s ease-in-out',
          },
          '&.MuiTableRow-head:hover': {
            backgroundColor: NAVY,
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
          backgroundColor: '#F4F6F9',
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
          backgroundColor: '#fff',
          borderRadius: '8px',
        },
      },
    },
    MuiInputBase: {
      styleOverrides: {
        root: {
          backgroundColor: '#fff',
          borderRadius: '8px',
        },
      },
    },
    MuiDataGrid: {
      styleOverrides: {
        columnHeader: {
          backgroundColor: NAVY,
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
});

export default theme;
