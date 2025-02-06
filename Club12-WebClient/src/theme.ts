import { createTheme } from "@mui/material/styles";

const theme = createTheme({
  palette: {
    primary: {
      main: "#FD6B00",
      light: "#E59700",
    },
    secondary: {
      main: "#2E2E2E",  
    },
    background: {
      default: "#f4f6f8",
      paper: "#ffffff",
    },
    text: {
      primary: "#333",
      secondary: "#666",
    },
  },
  typography: {
    fontFamily: "'Kanit', sans-serif",
    h6: {
      fontWeight: 600,
    },
  },
  components: {
    MuiTableRow: {
      styleOverrides: {
        root: {
          "&:hover": {
            backgroundColor: "#e3f2fd",  // Efecto hover en filas de tabla
          },
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: "8px",
          textTransform: "none",  // Evita mayúsculas forzadas en botones
        },
      },
    },
  },
});

export default theme;
