import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { redirect, Route, Routes } from 'react-router-dom';
import Home from './views/home/home';
import Login from './views/auth/login';
import HowWeAre from './views/home/howWeAre/howWeAre';
import NavMenu from './views/home/navMenu';
import { useAuth } from './modules/auth/hook/auth.hook';
import { useEffect } from 'react';
import MedicalRecord from './views/home/information/medicalRecord';
import Regulation from './views/home/information/regulation';
import theme from './theme';
import { ThemeProvider } from '@mui/material';
import TeamsGrid from './views/teams/commons/teamsGrid';
import TeamsDetails from './views/teams/commons/teamsDetails';
import TeamCreate from './views/teams/commons/teamsCreate';
import SanctionsTable from './views/sanctions/sanctions';
import Bracket1 from './views/core/bracket/bracket';
import { Tournament } from './views/tournament';
import { TournamentProvider } from './modules/tournament/context/tournament.context';
import { TeamProvider } from './modules/team/context/team.context';

function App() {
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    redirect('/');
  }, [isAuthenticated]);

  return (
    <ThemeProvider theme={theme}>
      <div>
        <NavMenu />
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/quienes-somos" element={<HowWeAre />} />
          <Route path="/ficha-medica" element={<MedicalRecord />} />
          <Route path="/reglamento" element={<Regulation />} />
          <Route
            path="/zona-a"
            element={
              <TournamentProvider>
                <Tournament id={'a-a-a-a-a'} />
              </TournamentProvider>
            }
            
          />
           {/* Teams Routes */}
           <Route
            path="/equipos/*"
            element={
              <TeamProvider>
                <Routes>
                  <Route path="" element={<TeamsGrid />} />
                  <Route path=":teamId" element={<TeamsDetails />} />
                  <Route path="crear" element={<TeamCreate />} />
                </Routes>
              </TeamProvider>
            }
          />

          <Route path="/sanciones" element={<SanctionsTable />} />
          <Route path="/braket" element={<Bracket1 />} />
          <Route path="/login" element={<Login />} />
        </Routes>
      </div>
    </ThemeProvider>
  );
}

export default App;
