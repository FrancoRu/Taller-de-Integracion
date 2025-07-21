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
import TeamsGrid from './views/teams/commons/teamsGrid';
import TeamsDetails from './views/teams/commons/teamsDetails';
import TeamCreate from './views/teams/commons/teamsCreate';
import SanctionsTable from './views/sanctions/sanctions';
import { TournamentProvider } from './modules/tournament/context/tournament.context';
import { TeamProvider } from './modules/team/context/team.context';
import { CreateTournament } from './views/tournament/CRUD/create-tournament';
import { TournamentDashboard } from './views/tournament/dashboard';
import { TournamentIndex } from './views/tournament';
import { DivisionIndex } from './views/division';
import { RoutesNavigationViews } from './views/core/routes-const';
import { EditTournament } from './views/tournament/CRUD/edit-tournament';
import { CreateDivision } from './views/division/CRUD/create-division';
import { DetailDidivion } from './views/division/CRUD/details-division';
import { EditDivision } from './views/division/CRUD/edit-division';

function App() {
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    redirect('/');
  }, [isAuthenticated]);

  return (
    <TournamentProvider>
      <div>
        <NavMenu />
        <Routes>
          <Route path={RoutesNavigationViews.Home} element={<Home />} />
          <Route
            path={RoutesNavigationViews.How_We_Are}
            element={<HowWeAre />}
          />
          <Route
            path={RoutesNavigationViews.Medical_Record}
            element={<MedicalRecord />}
          />
          <Route path={RoutesNavigationViews.Rules} element={<Regulation />} />
          <Route
            path={RoutesNavigationViews.Tournament}
            element={<TournamentIndex />}
          >
            <Route path="crear" element={<CreateTournament />} />
            <Route path=":id">
              <Route index element={<TournamentDashboard />} />
              <Route path="editar" element={<EditTournament />} />

              <Route
                path={`${RoutesNavigationViews.Division}/*`}
                element={<DivisionIndex />}
              >
                <Route path="crear" element={<CreateDivision />} />
                <Route path=":divisionId">
                  <Route index element={<DetailDidivion />} />
                  <Route path="editar" element={<EditDivision />} />
                </Route>
              </Route>
            </Route>
          </Route>

          <Route path="/equipos" element={<TeamsGrid />} />
          <Route path="/equipos/:teamId" element={<TeamsDetails />} />
          <Route path="/equipos/crear" element={<TeamCreate />} />
          <Route path="/sanciones" element={<SanctionsTable />} />
          <Route path="/login" element={<Login />} />
        </Routes>
      </div>
    </TournamentProvider>
  );
}

export default App;
