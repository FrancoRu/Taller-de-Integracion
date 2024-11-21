import { RouteProps } from 'react-router-dom';
import { lazy } from 'react';
import PrivateRoute from './views/core/privateRoute';
import Dashboard from './views/dashboard/dashboard';
import { BlogPostProvider } from './modules/blogPost/context/blogPost.context';

const Home = lazy(() => import('./views/home/home'));
const Login = lazy(() => import('./views/auth/login'));
const NotFound = lazy(() => import('./views/errors/NotFound'));

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
