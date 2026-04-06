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
import { UserProvider } from './modules/user/context/user.context';
import { DivisionProvider } from './modules/division/context/division.context';
import { PlayerProvider } from './modules/player/context/player.context';
import { StageProvider } from './modules/stage/context/stage.context';
import { MatchProvider } from './modules/match/context/match.context';
import { PlayerSanctionProvider } from './modules/playerSanction/context/playerSanction.context';
import { PlayerStatisticProvider } from './modules/playerStatistic/context/playerStatistic.context';
import { ScorerProvider } from './modules/scorer/context/scorer.context';

//process.loadEnvFile();
const queryClient = new QueryClient();

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <BrowserRouter>
          <ErrorProvider>
            <AuthProvider>
              <VenueProvider>
                <TeamProvider>
                  <PlayerProvider>
                    <UserProvider>
                      <TournamentProvider>
                        <DivisionProvider>
                          <StageProvider>
                            <MatchProvider>
                              <PlayerSanctionProvider>
                                <ScorerProvider>
                                  <PlayerStatisticProvider>
                                    <App />
                                  </PlayerStatisticProvider>
                                </ScorerProvider>
                              </PlayerSanctionProvider>
                            </MatchProvider>
                          </StageProvider>
                        </DivisionProvider>
                      </TournamentProvider>
                    </UserProvider>
                  </PlayerProvider>
                </TeamProvider>
              </VenueProvider>
            </AuthProvider>
          </ErrorProvider>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
