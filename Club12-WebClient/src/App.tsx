import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import '@fontsource/oswald/500.css';
import '@fontsource/oswald/600.css';
import '@fontsource/oswald/700.css';
import { lazy, ReactElement, Suspense } from 'react';
import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import routes from './modules/core/constants/routes';
import { APP_ROUTES } from './modules/core/constants/appRoutes';
import { useAuth } from './modules/auth/hook/auth.hook';
import SidebarLayout from './views/core/components/SidebarLayout';
import PublicLayout from './views/core/components/PublicLayout';
import { UserRolesType } from './modules/core/enum/user/userRolesType';
import InvalidToken from './views/core/errors/invalidToken';
import Forbidden from './views/core/errors/forbidden';
import NotFound from './views/core/errors/NotFound';
import PrivateRoute from './views/core/privateRoute';
import ScrollToTop from './views/core/components/ScrollToTop';
import GlobalLoadingOverlay from './views/core/components/GlobalLoadingOverlay';
import BlockingOverlay from './views/core/components/BlockingOverlay';

// Every route-level page is loaded on demand instead of shipped in the one
// main bundle every visitor downloads on first paint — the whole admin
// panel (Jugadores, Sanciones, the tournament wizard, …) was landing in a
// public visitor's browser just to render the home page. `NotFound`,
// `Forbidden` and `InvalidToken` stay eager: `App()` can return them
// directly from an early check below, outside the <Suspense> boundary the
// <Routes> tree sits in, and they're tiny enough that splitting them buys
// nothing.
const Home = lazy(() => import('./views/home/home'));
const PublicTeamPage = lazy(() => import('./views/home/teams/PublicTeamPage'));
const PublicSanctionsPage = lazy(() => import('./views/home/sanctions/PublicSanctionsPage'));
const PublicChampionsPage = lazy(() => import('./views/home/champions/PublicChampionsPage'));
const PublicMatchPage = lazy(() => import('./views/home/matches/PublicMatchPage'));
const PublicTournamentPage = lazy(() => import('./views/home/tournaments/PublicTournamentPage'));
const PublicSeasonsPage = lazy(() => import('./views/home/seasons/PublicSeasonsPage'));
const PublicSeasonPage = lazy(() => import('./views/home/seasons/PublicSeasonPage'));
const BlogPostDetailPage = lazy(() => import('./views/blogPost/BlogPostDetailPage'));
const BlogListPage = lazy(() => import('./views/blogPost/BlogListPage'));
const AddBlogPostForm = lazy(() => import('./views/blogPost/addBlogPostForm'));
const BlogPostsPage = lazy(() => import('./views/blogPost/BlogPostsPage'));
const BlogPostEditPage = lazy(() => import('./views/blogPost/BlogPostEditPage'));
const Login = lazy(() => import('./views/auth/login'));
const HowWeAre = lazy(() => import('./views/home/howWeAre/howWeAre'));
const MedicalRecord = lazy(() => import('./views/home/information/medicalRecord'));
const Regulation = lazy(() => import('./views/home/information/regulation'));
const PlayersPage = lazy(() => import('./views/player/PlayersPage'));
const PlayerPage = lazy(() => import('./views/player/PlayerPage'));
const TeamPage = lazy(() => import('./views/team/TeamPage'));
const TournamentPage = lazy(() => import('./views/tournament/TournamentPage'));
const TournamentEditPage = lazy(() => import('./views/tournament/TournamentEditPage'));
const TournamentWizardPage = lazy(() => import('./views/tournament/wizard/TournamentWizardPage'));
const DivisionPage = lazy(() => import('./views/division/divisionPage'));
const DivisionCreatePage = lazy(() => import('./views/division/divisionCreatePage'));
const DivisionEditPage = lazy(() => import('./views/division/divisionEditPage'));
const MatchPage = lazy(() => import('./views/match/matchPage'));
const UsersPage = lazy(() => import('./views/panel/UsersPage'));
const UserDetails = lazy(() => import('./views/user/userDetails'));
const CreateUser = lazy(() => import('./views/user/createUser'));
const InviteUser = lazy(() => import('./views/user/inviteUser'));
const EditUser = lazy(() => import('./views/user/editUser'));
const ChangePasswordPage = lazy(() => import('./views/panel/ChangePasswordPage'));
const StatisticsPage = lazy(() => import('./views/panel/StatisticsPage'));
const AuditLogsPage = lazy(() => import('./views/panel/AuditLogsPage'));
const DataAdministrationPage = lazy(() => import('./views/panel/DataAdministrationPage'));
const PasswordReset = lazy(() => import('./views/auth/passwordReset'));
const ForgotPassword = lazy(() => import('./views/auth/forgotPassword'));
const ActivateAccount = lazy(() => import('./views/auth/activateAccount'));
const TeamsPage = lazy(() => import('./views/team/TeamsPage'));
const ClubHistoryPage = lazy(() => import('./views/club/ClubHistoryPage'));
const PlayerSanctionsPage = lazy(() => import('./views/playerSanction/PlayerSanctionsPage'));
const PlayerSanctionPage = lazy(() => import('./views/playerSanction/PlayerSanctionPage'));
const PlayerSanctionEditPage = lazy(() => import('./views/playerSanction/playerSanctionEditPage'));
const VenuesPage = lazy(() => import('./views/venue/VenuesPage'));
const VenuePage = lazy(() => import('./views/venue/venuePage'));
const SeasonsPage = lazy(() => import('./views/season/SeasonsPage'));
const AdminSeasonDetailPage = lazy(() => import('./views/season/AdminSeasonDetailPage'));

const FIRST_TAB_BY_ROLE: Partial<Record<UserRolesType, string>> = {
  [UserRolesType.Owner]: APP_ROUTES.panelSeasons,
  [UserRolesType.Admin]: APP_ROUTES.panelSeasons,
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
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <PlayersPage />,
  },
  {
    path: APP_ROUTES.panelPlayer.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <PlayerPage />,
  },
  {
    path: APP_ROUTES.panelTeamDetail.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TeamPage />,
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
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TeamsPage title="Equipos" wrapInCard />,
  },
  {
    path: APP_ROUTES.panelClub.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <ClubHistoryPage />,
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
    path: APP_ROUTES.panelSeasons,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <SeasonsPage />,
  },
  {
    path: APP_ROUTES.panelSeason.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <AdminSeasonDetailPage />,
  },
  {
    path: APP_ROUTES.panelTournamentWizard,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <TournamentWizardPage />,
  },
  {
    path: APP_ROUTES.panelDivisionCreate,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DivisionCreatePage />,
  },
  {
    path: APP_ROUTES.panelDivisionEdit.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DivisionEditPage />,
  },
  {
    path: APP_ROUTES.panelDivision.pattern,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DivisionPage />,
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
    path: APP_ROUTES.panelUserInvite,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <InviteUser />,
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
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <StatisticsPage />,
  },
  {
    path: APP_ROUTES.panelAuditLogs,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <AuditLogsPage />,
  },
  {
    path: APP_ROUTES.panelDataAdministration,
    allowedRoles: [UserRolesType.Admin, UserRolesType.Owner],
    element: <DataAdministrationPage />,
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
  { path: APP_ROUTES.publicChampions, element: <PublicChampionsPage /> },
  { path: APP_ROUTES.publicMatch.pattern, element: <PublicMatchPage /> },
  { path: APP_ROUTES.publicSeasons, element: <PublicSeasonsPage /> },
  { path: APP_ROUTES.publicSeason.pattern, element: <PublicSeasonPage /> },
  // No `/torneos` flat listing route: it was never linked from any nav
  // (dead/unreachable, HU orphan-route audit) — every tournament is reached
  // via Temporadas -> season -> tournament instead.
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

  const defaultTab = FIRST_TAB_BY_ROLE[role] ?? APP_ROUTES.panelUsers;

  // Public pages render for EVERYONE — authenticated or not — so shareable
  // slug links (public tournament HU-14, blog post HU-13, team/match, HU-15)
  // keep working even while an admin is logged in. Previously the whole public
  // route tree was omitted for authenticated users, so any public slug URL fell
  // through to the panel catch-all and 404'd without ever hitting the API.
  //
  // Login (HU-02) and the 404/NotFound catch-all (HU-04) render without the
  // public header/footer, so they sit outside the PublicLayout chrome. The
  // admin panel is only mounted when authenticated, under one persistent
  // SidebarLayout (via <Outlet />) so the sidebar survives panel navigation.
  return (
    <>
      <ScrollToTop />
      <GlobalLoadingOverlay />
      <Suspense fallback={<BlockingOverlay open />}>
      <Routes>
      <Route element={<PublicLayout />}>
        {PUBLIC_ROUTES.map(({ path, element }) => (
          <Route key={path} path={path} element={element} />
        ))}
      </Route>
      <Route path={APP_ROUTES.login} element={<Login />} />
      <Route path={APP_ROUTES.forgotPassword} element={<ForgotPassword />} />
      <Route path={APP_ROUTES.activate} element={<ActivateAccount />} />

      {isAuthenticated && (
        <Route
          element={
            <SidebarLayout>
              <Outlet />
            </SidebarLayout>
          }
        >
          {ADMIN_ROUTES.filter(({ path }) => path !== '*').map(
            ({ path, element, allowedRoles }) => (
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
            )
          )}
          <Route
            path={APP_ROUTES.panel}
            element={<Navigate to={defaultTab} replace />}
          />
        </Route>
      )}

      <Route path="*" element={<NotFound />} />
      </Routes>
      </Suspense>
    </>
  );
}

export default App;
