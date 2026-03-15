import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { Navigate, Route, Routes } from 'react-router-dom';
import Home from './views/home/home';
import Login from './views/auth/login';
import HowWeAre from './views/home/howWeAre/howWeAre';
import NavMenu from './views/home/NavMenu/navMenu';
import { useAuth } from './modules/auth/hook/auth.hook';
import MedicalRecord from './views/home/information/medicalRecord';
import Regulation from './views/home/information/regulation';
import SidebarLayout from './views/core/components/SidebarLayout';
import PlayersPage from './views/panel/PlayersPage';
import TeamPage from './views/panel/TeamPage';
import TournamentPage from './views/panel/TournamentPage';
import TeamsPage from './views/panel/TeamsPage';
import TournamentsPage from './views/panel/TournamentsPage';
import UsersPage from './views/panel/UsersPage';
import ConfigurationPage from './views/panel/ConfigurationPage';
import StatisticsPage from './views/panel/StatisticsPage';
import { UserRolesType } from './modules/core/enum/user/userRolesType';

const FIRST_TAB_BY_ROLE: Partial<Record<UserRolesType, string>> = {
  [UserRolesType.TeamManager]: '/panel/jugadores',
  [UserRolesType.TournamentManager]: '/panel/torneo',
  [UserRolesType.Owner]: '/panel/torneos',
  [UserRolesType.Admin]: '/panel/usuarios',
};

function App() {
  const { isAuthenticated, role } = useAuth();

  if (isAuthenticated) {
    const defaultTab = FIRST_TAB_BY_ROLE[role] ?? '/panel/usuarios';
    return (
      <SidebarLayout>
        <Routes>
          <Route path="/panel/jugadores" element={<PlayersPage />} />
          <Route path="/panel/equipo" element={<TeamPage />} />
          <Route path="/panel/torneo" element={<TournamentPage />} />
          <Route path="/panel/equipos" element={<TeamsPage />} />
          <Route path="/panel/torneos" element={<TournamentsPage />} />
          <Route path="/panel/usuarios" element={<UsersPage />} />
          <Route path="/panel/configuracion" element={<ConfigurationPage />} />
          <Route path="/panel/estadisticas" element={<StatisticsPage />} />
          <Route path="/panel" element={<Navigate to={defaultTab} replace />} />
          <Route path="*" element={<Navigate to={defaultTab} replace />} />
        </Routes>
      </SidebarLayout>
    );
  }

  return (
    <>
      <NavMenu />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/quienes-somos" element={<HowWeAre />} />
        <Route path="/ficha-medica" element={<MedicalRecord />} />
        <Route path="/reglamento" element={<Regulation />} />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  );
}

export default App;
