import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import { AuthProvider } from './modules/auth/context/auth.context';
import { BrowserRouter } from 'react-router-dom';
import { ErrorProvider } from './modules/error/context/error.context';
import { ThemeProvider } from '@emotion/react';
import theme from './theme';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TournamentProvider } from './modules/tournament/context/tournament.context';
import { VenueProvider } from './modules/venue/context/venue.context';
import { TeamProvider } from './modules/team/context/team.context';

//process.loadEnvFile();
const queryClient = new QueryClient();

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <BrowserRouter>
          <ErrorProvider>
            <AuthProvider>
              <TournamentProvider>
                <VenueProvider>
                  <TeamProvider>
                    <App />
                  </TeamProvider>
                </VenueProvider>
              </TournamentProvider>
            </AuthProvider>
          </ErrorProvider>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
