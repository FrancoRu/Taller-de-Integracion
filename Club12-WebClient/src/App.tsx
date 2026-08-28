import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import '@fontsource/oswald/500.css';
import '@fontsource/oswald/600.css';
import '@fontsource/oswald/700.css';
import { ReactElement } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './views/home/home';
import PublicTeamPage from './views/home/teams/PublicTeamPage';
import PublicSanctionsPage from './views/home/sanctions/PublicSanctionsPage';
import PublicMatchPage from './views/home/matches/PublicMatchPage';
import PublicTournamentsPage from './views/home/tournaments/PublicTournamentsPage';
import PublicTournamentPage from './views/home/tournaments/PublicTournamentPage';
import BlogPostDetailPage from './views/blogPost/BlogPostDetailPage';
import BlogListPage from './views/blogPost/BlogListPage';
import AddBlogPostForm from './views/blogPost/addBlogPostForm';
import BlogPostsPage from './views/blogPost/BlogPostsPage';
import BlogPostEditPage from './views/blogPost/BlogPostEditPage';
import Login from './views/auth/login';
import routes from './modules/core/constants/routes';
import { APP_ROUTES } from './modules/core/constants/appRoutes';
import HowWeAre from './views/home/howWeAre/howWeAre';
import { useAuth } from './modules/auth/hook/auth.hook';
import MedicalRecord from './views/home/information/medicalRecord';
import Regulation from './views/home/information/regulation';
import SidebarLayout from './views/core/components/SidebarLayout';
import PublicLayout from './views/core/components/PublicLayout';
import PlayersPage from './views/player/PlayersPage';
import PlayerPage from './views/player/PlayerPage';
import TeamPage from './views/team/TeamPage';
import TournamentPage from './views/tournament/TournamentPage';
import TournamentEditPage from './views/tournament/TournamentEditPage';
import TournamentsPage from './views/tournament/TournamentsPage';
import TournamentWizardPage from './views/tournament/wizard/TournamentWizardPage';
import DivisionPage from './views/division/divisionPage';
import DivisionsPage from './views/division/divisionsPage';
import DivisionCreatePage from './views/division/divisionCreatePage';
import StagePage from './views/stage/stagePage';
import StageCreatePage from './views/stage/stageCreatePage';
import StageEditPage from './views/stage/stageEditPage';
import StagesPage from '@/views/stage/stagesPage';
import MatchPage from './views/match/matchPage';
import MatchesPage from './views/match/matchesPage';
import MatchCreatePage from './views/match/matchCreatePage';
import UsersPage from './views/panel/UsersPage';
import UserDetails from './views/user/userDetails';
import CreateUser from './views/user/createUser';
import EditUser from './views/user/editUser';
import ChangePasswordPage from './views/panel/ChangePasswordPage';
import StatisticsPage from './views/panel/StatisticsPage';
import AuditLogsPage from './views/panel/AuditLogsPage';
import TestDataPage from './views/panel/TestDataPage';
import { UserRolesType } from './modules/core/enum/user/userRolesType';
import InvalidToken from './views/core/errors/invalidToken';
import Forbidden from './views/core/errors/forbidden';
import NotFound from './views/core/errors/NotFound';
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
  [UserRolesType.Owner]: APP_ROUTES.panelTournaments,
  [UserRolesType.Admin]: APP_ROUTES.panelUsers,
};

interface AdminRouteConfig {
  path: string;
  element: ReactElement;
  allowedRoles?: UserRolesType[];
}

const ADMIN_ROUTES: AdminRouteConfig[] = [
  { path: APP_ROUTES.passwordReset, element: <PasswordReset /> },
  {
    path: APP_ROUTES.panelPlayers,
    allowedRoles: [UserRolesType.Owner],
    element: <PlayersPage />,
  },
  {
    path: APP_ROUTES.panelPlayer.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <PlayerPage />,
  },
  {
    path: APP_ROUTES.panelTeam,
    allowedRoles: [UserRolesType.Owner],
    element: <TeamPage />,
  },
  {
    path: APP_ROUTES.panelTeamDetail.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TeamPage />,
  },
  {
    path: APP_ROUTES.panelTournament,
    allowedRoles: [UserRolesType.Owner],
    element: <TournamentPage />,
  },
  {
    path: APP_ROUTES.panelTournamentDetail.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TournamentPage />,
  },
  {
    path: APP_ROUTES.panelTournamentEdit.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TournamentEditPage />,
  },
  {
    path: APP_ROUTES.panelTeams,
    allowedRoles: [UserRolesType.Owner],
    element: <TeamsPage title="Equipos" wrapInCard />,
  },
  {
    path: APP_ROUTES.panelTeamRegister,
    allowedRoles: [UserRolesType.Owner],
    element: <TeamRegisterPage />,
  },
  {
    path: APP_ROUTES.panelSanctions,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <PlayerSanctionsPage />,
  },
  {
    path: APP_ROUTES.panelSanction.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <PlayerSanctionPage />,
  },
  {
    path: APP_ROUTES.panelSanctionEdit.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <PlayerSanctionEditPage />,
  },
  {
    path: APP_ROUTES.panelScorers,
    allowedRoles: [UserRolesType.Owner],
    element: <ScorersPage />,
  },
  {
    path: APP_ROUTES.panelVenues,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <VenuesPage />,
  },
  {
    path: APP_ROUTES.panelVenue.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <VenuePage />,
  },
  {
    path: APP_ROUTES.panelTournaments,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TournamentsPage />,
  },
  {
    path: APP_ROUTES.panelTournamentWizard,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TournamentWizardPage />,
  },
  {
    path: APP_ROUTES.panelDivisions,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DivisionsPage wrapInCard />,
  },
  {
    path: APP_ROUTES.panelDivisionCreate,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DivisionCreatePage />,
  },
  {
    path: APP_ROUTES.panelDivision.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DivisionPage />,
  },
  {
    path: APP_ROUTES.panelStages,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <StagesPage wrapInCard />,
  },
  {
    path: APP_ROUTES.panelStageCreate,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <StageCreatePage />,
  },
  {
    path: APP_ROUTES.panelStageEdit.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <StageEditPage />,
  },
  {
    path: APP_ROUTES.panelStage.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <StagePage />,
  },
  {
    path: APP_ROUTES.panelMatches,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <MatchesPage wrapInCard />,
  },
  {
    path: APP_ROUTES.panelMatchCreate,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <MatchCreatePage />,
  },
  {
    path: APP_ROUTES.panelMatch.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <MatchPage />,
  },
  {
    path: APP_ROUTES.panelBlog,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <BlogPostsPage />,
  },
  {
    path: APP_ROUTES.panelBlogCreate,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <AddBlogPostForm />,
  },
  {
    path: APP_ROUTES.panelBlogEdit.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <BlogPostEditPage />,
  },
  {
    path: APP_ROUTES.panelUsers,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <UsersPage />,
  },
  {
    path: APP_ROUTES.panelUserCreate,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <CreateUser />,
  },
  {
    path: APP_ROUTES.panelUserEdit.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <EditUser />,
  },
  {
    path: APP_ROUTES.panelUser.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <UserDetails />,
  },
  {
    path: APP_ROUTES.panelSettings,
    element: <Navigate to={APP_ROUTES.panelChangePassword} replace />,
  },
  {
    path: APP_ROUTES.panelChangePassword,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <ChangePasswordPage />,
  },
  {
    path: APP_ROUTES.panelEditProfile,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <EditUser />,
  },
  {
    path: APP_ROUTES.panelStatistics,
    allowedRoles: [UserRolesType.Admin],
    element: <StatisticsPage />,
  },
  {
    path: APP_ROUTES.panelAuditLogs,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <AuditLogsPage />,
  },
  {
    path: APP_ROUTES.panelTest,
    allowedRoles: [UserRolesType.Admin],
    element: <TestDataPage />,
  },
  { path: '*', element: <NotFound /> },
];

interface PublicRouteConfig {
  path: string;
  element: ReactElement;
}

const PUBLIC_ROUTES: PublicRouteConfig[] = [
  { path: APP_ROUTES.passwordReset, element: <PasswordReset /> },
  { path: APP_ROUTES.home, element: <Home /> },
  { path: APP_ROUTES.quienesSomos, element: <HowWeAre /> },
  { path: APP_ROUTES.fichaMedica, element: <MedicalRecord /> },
  { path: APP_ROUTES.reglamento, element: <Regulation /> },
  { path: APP_ROUTES.publicTeam.pattern, element: <PublicTeamPage /> },
  { path: APP_ROUTES.publicSanctions, element: <PublicSanctionsPage /> },
  { path: APP_ROUTES.publicMatch.pattern, element: <PublicMatchPage /> },
  { path: APP_ROUTES.publicTournaments, element: <PublicTournamentsPage /> },
  {
    path: APP_ROUTES.publicTournament.pattern,
    element: <PublicTournamentPage />,
  },
  { path: APP_ROUTES.publicBlog, element: <BlogListPage /> },
  { path: APP_ROUTES.blogPost.pattern, element: <BlogPostDetailPage /> },
];

function App() {
  const { isAuthenticated, role } = useAuth();
  const location = useLocation();

  if (location.pathname === APP_ROUTES.forbidden) return <Forbidden />;
  if (location.pathname === routes.tokenInvalido) return <InvalidToken />;

  if (isAuthenticated) {
    const defaultTab = FIRST_TAB_BY_ROLE[role] ?? APP_ROUTES.panelUsers;
    return (
      <SidebarLayout>
        <Routes>
          {ADMIN_ROUTES.map(({ path, element, allowedRoles }) => (
            <Route
              key={path}
              path={path}
              element={
                allowedRoles ? (
                  <PrivateRoute allowedRoles={allowedRoles}>
                    {element}
                  </PrivateRoute>
                ) : (
                  element
                )
              }
            />
          ))}
          <Route
            path={APP_ROUTES.panel}
            element={<Navigate to={defaultTab} replace />}
          />
        </Routes>
      </SidebarLayout>
    );
  }

  // Login (HU-02) and the 404/NotFound catch-all (HU-04) must render without
  // the public header/footer, so they sit outside the PublicLayout chrome.
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        {PUBLIC_ROUTES.map(({ path, element }) => (
          <Route key={path} path={path} element={element} />
        ))}
      </Route>
      <Route path={APP_ROUTES.login} element={<Login />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

export default App;
