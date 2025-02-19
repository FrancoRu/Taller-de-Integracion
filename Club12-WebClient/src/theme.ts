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
      default: "#ffa05b",
      paper: "#ffe7d6",
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
            backgroundColor: "#ffc79e",
            transition: "background-color 0.3s ease-in-out",  // Color hover para todas las filas (incluye header)
          },  
          "&.MuiTableRow-head:hover": {
            backgroundColor: "#E59700", // Evita el hover en el header
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
    MuiCssBaseline: {
      styleOverrides: { 
        body: {
          backgroundColor: "#f4f6f8 !important",
          // backgroundImage: "url('/assets/background.jpg')", // Ruta de tu imagen de fondo
          // backgroundSize: "cover", // Para que la imagen cubra toda la pantalla
          // backgroundPosition: "center", // Centra la imagen
          // backgroundAttachment: "fixed", // Fija la imagen al fondo al hacer scroll
          border: "5px solid red !important", 
        },
      },
    },
  },
});

export default theme;
