import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { redirect, Route, Routes } from 'react-router-dom';
import Home from './views/home/home';
import Login from './views/auth/login';
import HowWeAre from './views/home/howWeAre/howWeAre';
import NavMenu from './views/home/NavMenu/navMenu';
import { useAuth } from './modules/auth/hook/auth.hook';
import { useEffect } from 'react';
import MedicalRecord from './views/home/information/medicalRecord';
import Regulation from './views/home/information/regulation';
import { TournamentProvider } from './modules/tournament/context/tournament.context';
import { CreateTournament } from './views/tournament/CRUD/create-tournament';
import { TournamentDashboard } from './views/tournament/dashboard';
import { TournamentIndex } from './views/tournament';
import { DivisionIndex } from './views/division';
import { RoutesNavigationViews } from './views/core/routes-const';
import { EditTournament } from './views/tournament/CRUD/edit-tournament';
import { CreateDivision } from './views/division/CRUD/create-division';
import { DetailDidivion } from './views/division/CRUD/details-division';
import { EditDivision } from './views/division/CRUD/edit-division';
import { DivisionProvider } from './modules/division/context/division.context';
import { StageProvider } from './modules/stage/context/stage.context';
import { StageIndex } from './views/stage';
import { CreateStage } from './views/stage/CRUD/create-stage';
import { DetailStage } from './views/stage/CRUD/details-stage';
import { EditStage } from './views/stage/CRUD/edit-stage';
import { MatchProvider } from './modules/match/context/match.context';
import { MatchIndex } from './views/match';
import { CreateMatch } from './views/match/CRUD/create-match';
import { DetailMatch } from './views/match/CRUD/details-match';
import { EditMatch } from './views/match/CRUD/edit-match';
import { VenueIndex } from './views/venue';
import { CreateVenue } from './views/venue/CRUD/create-venue';
import { DetailVenue } from './views/venue/CRUD/detail-venue';
import { EditVenue } from './views/venue/CRUD/edit-venue';
import { VenueDashboard } from './views/venue/dashboard';
import { VenueProvider } from './modules/venue/context/venue.context';
import { PlayerIndex } from './views/player';
import { PlayerDashboard } from './views/player/dashboard';
import { CreatePlayer } from './views/player/CRUD/create-player';
import { DetailPlayer } from './views/player/CRUD/detail-player';
import { EditPlayer } from './views/player/CRUD/edit-player';
import { TeamProvider } from './modules/team/context/team.context';
import { PlayerProvider } from './modules/player/context/player.context';
import { TeamIndex } from './views/team';
import { TeamDashboard } from './views/team/dashboard';
import { DetailTeam } from './views/team/CRUD/detail-team';
import { EditTeam } from './views/team/CRUD/edit-team';
import { CreateTeam } from './views/team/CRUD/create-team';

function App() {
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    redirect('/');
  }, [isAuthenticated]);

  return (
    <TournamentProvider>
      <DivisionProvider>
        <StageProvider>
          <MatchProvider>
            <VenueProvider>
              <TeamProvider>
                <PlayerProvider>
                  <div>
                    <NavMenu />
                    <Routes>
                      <Route
                        path={RoutesNavigationViews.Home}
                        element={<Home />}
                      />
                      <Route
                        path={RoutesNavigationViews.How_We_Are}
                        element={<HowWeAre />}
                      />
                      <Route
                        path={RoutesNavigationViews.Medical_Record}
                        element={<MedicalRecord />}
                      />
                      <Route
                        path={RoutesNavigationViews.Rules}
                        element={<Regulation />}
                      />
                      <Route
                        path={RoutesNavigationViews.Tournament}
                        element={<TournamentIndex />}
                      >
                        <Route path="crear" element={<CreateTournament />} />
                        <Route path=":id">
                          <Route index element={<TournamentDashboard />} />
                          <Route path="editar" element={<EditTournament />} />
                        </Route>
                      </Route>
                      <Route
                        path={`${RoutesNavigationViews.Division}`}
                        element={<DivisionIndex />}
                      >
                        <Route path="crear" element={<CreateDivision />} />
                        <Route path=":id">
                          <Route index element={<DetailDidivion />} />
                          <Route path="editar" element={<EditDivision />} />
                        </Route>
                      </Route>
                      <Route
                        path={`${RoutesNavigationViews.Stage}`}
                        element={<StageIndex />}
                      >
                        <Route path="crear" element={<CreateStage />} />
                        <Route path=":id">
                          <Route index element={<DetailStage />} />
                          <Route path="editar" element={<EditStage />} />
                        </Route>
                      </Route>
                      <Route
                        path={`${RoutesNavigationViews.Match}`}
                        element={<MatchIndex />}
                      >
                        <Route path="crear" element={<CreateMatch />} />
                        <Route path=":id">
                          <Route index element={<DetailMatch />} />
                          <Route path="editar" element={<EditMatch />} />
                        </Route>
                      </Route>
                      <Route
                        path={`${RoutesNavigationViews.Venue}`}
                        element={<VenueIndex />}
                      >
                        <Route index element={<VenueDashboard />} />
                        <Route path="crear" element={<CreateVenue />} />
                        <Route path=":id">
                          <Route index element={<DetailVenue />} />
                          <Route path="editar" element={<EditVenue />} />
                        </Route>
                      </Route>
                      <Route
                        path={`${RoutesNavigationViews.Team}`}
                        element={<TeamIndex />}
                      >
                        <Route index element={<TeamDashboard />} />
                        <Route path="crear" element={<CreateTeam />} />
                        <Route path=":id">
                          <Route index element={<DetailTeam />} />
                          <Route path="editar" element={<EditTeam />} />
                        </Route>
                      </Route>
                      <Route
                        path={`${RoutesNavigationViews.Player}`}
                        element={<PlayerIndex />}
                      >
                        <Route index element={<PlayerDashboard />} />
                        <Route path="crear" element={<CreatePlayer />} />
                        <Route path=":id">
                          <Route index element={<DetailPlayer />} />
                          <Route path="editar" element={<EditPlayer />} />
                        </Route>
                      </Route>
                      {/* <Route path="/equipos" element={<TeamsGrid />} />
                      <Route
                        path="/equipos/:teamId"
                        element={<TeamsDetails />}
                      />
                      <Route path="/equipos/crear" element={<TeamCreate />} />
                      <Route path="/sanciones" element={<SanctionsTable />} /> */}
                      <Route path="/login" element={<Login />} />
                    </Routes>
                  </div>
                </PlayerProvider>
              </TeamProvider>
            </VenueProvider>
          </MatchProvider>
        </StageProvider>
      </DivisionProvider>
    </TournamentProvider>
  );
}

export default App;
