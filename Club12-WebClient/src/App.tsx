import React from 'react';
import { ThemeProvider } from '@emotion/react';
import { CssBaseline } from '@mui/material/';
import { Route, Routes, Navigate } from 'react-router-dom';
import theme from './styles/theme';
import './styles/main.css';
import { SignIn } from './views/access/SignIn';
import { useAuth } from './hooks/auth/useAuth';
import Layout from './views/layouts/Layout';
import { Players } from './views/players/palyers';
import { Teams } from './views/teams/teams';
import { Home } from './views/home/home';

function App() {
  const { isAuthenticated } = useAuth();
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Home />} />
          <Route path="players" element={<Players />} />
          <Route path="teams" element={<Teams />} />
          <Route path="*" element={<Home />} />
          <Route
            path="login"
            element={isAuthenticated ? <Navigate to="/" /> : <SignIn />}
          />
        </Route>
        
      </Routes>
    </ThemeProvider>
  );
}

export default App;
