import { RouteProps } from 'react-router-dom';
import { lazy } from 'react';
import PrivateRoute from './views/core/privateRoute';
import Dashboard from './views/dashboard/dashboard';
import { BlogPostProvider } from './modules/blogPost/context/blogPost.context';
import SanctionsTable from './views/sanctions/sanctions';

const Home = lazy(() => import('./views/home/home'));
const Login = lazy(() => import('./views/auth/login'));
const NotFound = lazy(() => import('./views/core/errors/NotFound'));
const TeamsGrid = lazy(() => import('./views/teams/commons/teamsGrid'));
const TeamDetails = lazy(() => import('./views/teams/commons/teamsDetails'));
const TeamCreate = lazy(() => import('./views/teams/commons/teamsCreate'));

export type AppRoute = RouteProps & {
  element: JSX.Element;
};

export const routes: AppRoute[] = [
  {
    path: '/',
    element: (
      <BlogPostProvider>
        <Home />
      </BlogPostProvider>
    ),
  },
  {
    path: '/login',
    element: <Login />,
  },
  {
    path: '/teams',
    element: (
      <BlogPostProvider>
        <TeamsGrid />
      </BlogPostProvider>
    ),
  },
  {
    path: '/teams/:teamId',
    element: (
      <BlogPostProvider>
        <TeamDetails />
      </BlogPostProvider>
    ),
  },
  {
    path: '/teams/create',
    element: (
      <BlogPostProvider>
        <TeamCreate />
      </BlogPostProvider>
    ),
  },
  {
    path: '/sanciones',
    element: (
      <BlogPostProvider>
        <SanctionsTable />
      </BlogPostProvider>
    ),
  },
  // {
  //   path: '/braket',
  //   element: <BlogPostProvider>{/* <Braket1 /> */}</BlogPostProvider>,
  // },
  {
    path: '/dashboard',
    element: (
      <BlogPostProvider>
        <PrivateRoute>
          <Dashboard />
        </PrivateRoute>
      </BlogPostProvider>
    ),
  },
  {
    path: '*',
    element: <NotFound />,
  },
];
