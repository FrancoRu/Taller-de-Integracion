import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './views/home/home';
import PublicTeamsPage from './views/home/teams/PublicTeamsPage';
import PublicTeamPage from './views/home/teams/PublicTeamPage';
import PublicScorersPage from './views/home/scorers/PublicScorersPage';
import PublicSanctionsPage from './views/home/sanctions/PublicSanctionsPage';
import PublicMatchesPage from './views/home/matches/PublicMatchesPage';
import PublicTournamentsPage from './views/home/tournaments/PublicTournamentsPage';
import PublicTournamentPage from './views/home/tournaments/PublicTournamentPage';
import Login from './views/auth/login';
import HowWeAre from './views/home/howWeAre/howWeAre';
import NavMenu from './views/home/NavMenu/navMenu';
import { useAuth } from './modules/auth/hook/auth.hook';
import MedicalRecord from './views/home/information/medicalRecord';
import Regulation from './views/home/information/regulation';
import SidebarLayout from './views/core/components/SidebarLayout';
import PlayersPage from './views/player/PlayersPage';
import PlayerPage from './views/player/PlayerPage';
import TeamPage from './views/team/TeamPage';
import TournamentPage from './views/tournament/TournamentPage';
import TournamentEditPage from './views/tournament/TournamentEditPage';
import TournamentsPage from './views/tournament/TournamentsPage';
import DivisionPage from './views/division/divisionPage';
import DivisionsPage from './views/division/divisionsPage';
import StagePage from './views/stage/stagePage';
import StageCreatePage from './views/stage/stageCreatePage';
import StageEditPage from './views/stage/stageEditPage';
import StagesPage from '@/views/stage/stagesPage';
import MatchPage from './views/match/matchPage';
import MatchesPage from './views/match/matchesPage';
import UsersPage from './views/panel/UsersPage';
import UserDetails from './views/user/userDetails';
import CreateUser from './views/user/createUser';
import EditUser from './views/user/editUser';
import ChangePasswordPage from './views/panel/ChangePasswordPage';
import StatisticsPage from './views/panel/StatisticsPage';
import { UserRolesType } from './modules/core/enum/user/userRolesType';
import InvalidToken from './views/core/errors/invalidToken';
import Forbidden from './views/core/errors/forbidden';
import PasswordReset from './views/auth/passwordReset';
import PrivateRoute from './views/core/privateRoute';
import TeamsPage from './views/team/TeamsPage';
import TeamRegisterPage from './views/team/TeamRegisterPage';
import PlayerSanctionsPage from './views/playerSanction/PlayerSanctionsPage';
import PlayerSanctionPage from './views/playerSanction/PlayerSanctionPage';
import PlayerSanctionEditPage from './views/playerSanction/playerSanctionEditPage';
import VenuesPage from './views/venue/VenuesPage';
import VenuePage from './views/venue/venuePage';
import ScorersPage from './views/scorer/scorersPage';

const FIRST_TAB_BY_ROLE: Partial<Record<UserRolesType, string>> = {
  [UserRolesType.TeamManager]: '/panel/jugadores',
  [UserRolesType.TournamentManager]: '/panel/torneos',
  [UserRolesType.Owner]: '/panel/torneos',
  [UserRolesType.Admin]: '/panel/usuarios',
};

function App() {
  const { isAuthenticated, role } = useAuth();
  const location = useLocation();

  if (location.pathname === '/forbidden') return <Forbidden />;
  if (location.pathname === '/token-invalido') return <InvalidToken />;

  if (isAuthenticated) {
    const defaultTab = FIRST_TAB_BY_ROLE[role] ?? '/panel/usuarios';
    return (
      <SidebarLayout>
        <Routes>
          <Route path="/auth/password-reset" element={<PasswordReset />} />
          <Route
            path="/panel/jugadores"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.TeamManager, UserRolesType.Owner]}
              >
                <PlayersPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/jugadores/:playerId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Admin,
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                  UserRolesType.TeamManager,
                ]}
              >
                <PlayerPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/equipo"
            element={
              <PrivateRoute allowedRoles={[UserRolesType.TeamManager]}>
                <TeamPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/equipos/:teamId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TeamManager,
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <TeamPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/torneo"
            element={
              <PrivateRoute allowedRoles={[UserRolesType.TournamentManager]}>
                <TournamentPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/torneos/:tournamentId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Admin,
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <TournamentPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/torneos/:tournamentId/editar"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Admin,
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <TournamentEditPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/equipos"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <TeamsPage title="Equipos" wrapInCard />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/registro-equipos"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <TeamRegisterPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/sanciones"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <PlayerSanctionsPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/sanciones/:playerSanctionId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <PlayerSanctionPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/sanciones/editar/:playerSanctionId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <PlayerSanctionEditPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/puntuaciones"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <ScorersPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/canchas"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <VenuesPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/canchas/:venueId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.TournamentManager,
                  UserRolesType.Owner,
                ]}
              >
                <VenuePage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/torneos"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Admin,
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <TournamentsPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/divisiones"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <DivisionsPage wrapInCard />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/divisiones/:divisionId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <DivisionPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/fases"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <StagesPage wrapInCard />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/fases/crear"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <StageCreatePage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/fases/editar/:stageId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <StageEditPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/fases/:stageId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <StagePage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/partidos"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <MatchesPage wrapInCard />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/partidos/:matchId"
            element={
              <PrivateRoute
                allowedRoles={[
                  UserRolesType.Owner,
                  UserRolesType.TournamentManager,
                ]}
              >
                <MatchPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/usuarios"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.Admin, UserRolesType.Owner]}
              >
                <UsersPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/usuarios/crear"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.Admin, UserRolesType.Owner]}
              >
                <CreateUser />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/usuarios/:userId/editar"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.Admin, UserRolesType.Owner]}
              >
                <EditUser />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/usuarios/:userId"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.Admin, UserRolesType.Owner]}
              >
                <UserDetails />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/configuracion"
            element={
              <Navigate to="/panel/configuracion/cambiar-password" replace />
            }
          />
          <Route
            path="/panel/configuracion/cambiar-password"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.Admin, UserRolesType.Owner]}
              >
                <ChangePasswordPage />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/configuracion/editar-perfil"
            element={
              <PrivateRoute
                allowedRoles={[UserRolesType.Admin, UserRolesType.Owner]}
              >
                <EditUser />
              </PrivateRoute>
            }
          />
          <Route
            path="/panel/estadisticas"
            element={
              <PrivateRoute allowedRoles={[UserRolesType.Admin]}>
                <StatisticsPage />
              </PrivateRoute>
            }
          />
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
        <Route path="/auth/password-reset" element={<PasswordReset />} />
        <Route path="/" element={<Home />} />
        <Route path="/quienes-somos" element={<HowWeAre />} />
        <Route path="/ficha-medica" element={<MedicalRecord />} />
        <Route path="/reglamento" element={<Regulation />} />
        <Route path="/equipos" element={<PublicTeamsPage />} />
        <Route path="/equipos/:teamId" element={<PublicTeamPage />} />
        <Route path="/goleadores" element={<PublicScorersPage />} />
        <Route path="/sanciones" element={<PublicSanctionsPage />} />
        <Route path="/partidos" element={<PublicMatchesPage />} />
        <Route path="/torneos" element={<PublicTournamentsPage />} />
        <Route path="/torneos/:tournamentId" element={<PublicTournamentPage />} />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  );
}

export default App;
