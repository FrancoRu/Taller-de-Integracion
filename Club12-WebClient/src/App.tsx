import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";

import { Navigate, Route, Routes } from "react-router-dom";

import { ThemeProvider } from "@emotion/react";
import { CssBaseline } from "@mui/material/";
import theme from "./styles/theme";
import "./styles/main.css";
import { SignIn } from "./views/access/SignIn";
import { Home } from "./views/dashboard/home";
import { useAuth } from "./hooks/auth/useAuth";

function App() {
  const { isAuthenticated } = useAuth();
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route
          path="/login"
          element={isAuthenticated ? <Navigate to="/" /> : <SignIn />}
        />
      </Routes>
    </ThemeProvider>
  );
}

export default App;
