import { RouteProps } from "react-router-dom";
import { lazy } from "react";
import PrivateRoute from "./views/core/privateRoute";
import Dashboard from "./views/dashboard/dashboard";
import { BlogPostProvider } from "./modules/blogPost/context/blogPost.context";

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
      <BlogPostProvider>  {/* Wrap Home with BlogPostProvider */}
        <Home />
      </BlogPostProvider>
    ),  // Home is publicly accessible
  },
  {
    path: '/login',
    element: <Login />,  // Login is publicly accessible
  },
  {
    path: '/dashboard',  // Protected Dashboard route
    element: (
      <BlogPostProvider> {/* Wrap Dashboard with BlogPostProvider */}
        <PrivateRoute>
          <Dashboard />
        </PrivateRoute>
      </BlogPostProvider>
    ),
  },
  {
    path: '*',
    element: <NotFound />,  // Fallback for unmatched routes
  },
];
