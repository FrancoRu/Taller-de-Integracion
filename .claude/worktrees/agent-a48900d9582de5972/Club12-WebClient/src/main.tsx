import { ComponentType, ReactNode } from 'react';
import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import { AuthProvider } from './modules/auth/context/auth.context';
import { BrowserRouter } from 'react-router-dom';
import { ErrorProvider } from './modules/error/context/error.context';
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
import { BlogPostProvider } from './modules/blogPost/context/blogPost.context';
import ErrorBoundary from './views/core/errors/error-boundary';
import ComposeProviders from './views/core/components/ComposeProviders';
import QueryProvider from './views/core/components/QueryProvider';
import ThemedProvider from './views/core/components/ThemedProvider';

const providers: ComponentType<{ children: ReactNode }>[] = [
  ErrorBoundary,
  QueryProvider,
  ThemedProvider,
  BrowserRouter,
  ErrorProvider,
  AuthProvider,
  VenueProvider,
  TeamProvider,
  PlayerProvider,
  UserProvider,
  TournamentProvider,
  DivisionProvider,
  StageProvider,
  MatchProvider,
  PlayerSanctionProvider,
  ScorerProvider,
  PlayerStatisticProvider,
  BlogPostProvider,
];

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <ComposeProviders providers={providers}>
      <App />
    </ComposeProviders>
  </React.StrictMode>
);
