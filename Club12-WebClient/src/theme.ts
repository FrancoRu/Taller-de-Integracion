import { orange } from '@mui/material/colors';
import { createTheme } from '@mui/material/styles';

const baseTheme = createTheme({
  palette: {
    primary: {
      main: '#FD6B00',
      light: '#E59700',
    },
    secondary: {
      main: '#2E2E2E',
    },
    background: {
      default: '#ffa05b',
      paper: '#ffe7d6',
    },
    text: {
      primary: '#333',
      secondary: '#666',
    },
  },
  typography: {
    fontFamily: "'Kanit', sans-serif",
    h6: {
      fontWeight: 600,
    },
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
    MuiTableRow: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: '#ffc79e',
            transition: 'background-color 0.3s ease-in-out',
          },
          '&.MuiTableRow-head:hover': {
            backgroundColor: '#E59700',
          },
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: '8px',
          textTransform: 'none',
          marginTop: '16px',
          color: 'white',
          fontWeight: 'bold',
        },
      },
    },
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: '#f4f6f8 !important',
          border: '5px solid red !important',
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
        columnHeaderTitle: {
          fontWeight: 'bold',
          fontSize: '1rem',
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
            backgroundColor: orange[700],
            '&:hover': {
              backgroundColor: orange[600],
            },
          },
        },
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          '&.Mui-selected': {
            backgroundColor: orange[700],
            '&:hover': {
              backgroundColor: orange[600],
            },
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          padding: baseTheme.spacing(3),
          boxShadow: baseTheme.shadows[5],
        },
      },
    },
  },
});

export default theme;
