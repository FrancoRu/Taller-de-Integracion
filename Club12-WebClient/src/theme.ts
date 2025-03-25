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
            transition: "background-color 0.3s ease-in-out",  
          },  
          "&.MuiTableRow-head:hover": {
            backgroundColor: "#E59700", 
          },
        },
      },
    },
    
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: "8px",
          textTransform: "none",  
        },
      },
    },
    MuiCssBaseline: {
      styleOverrides: { 
        body: {
          backgroundColor: "#f4f6f8 !important",
          border: "5px solid red !important", 
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          backgroundColor: "#fff", 
          borderRadius: "8px",
        },
      },
    },
    MuiInputBase: {
      styleOverrides: {
        root: {
          backgroundColor: "#fff", 
          borderRadius: "8px",
        },
      },
    },
  },
});

export default theme;
