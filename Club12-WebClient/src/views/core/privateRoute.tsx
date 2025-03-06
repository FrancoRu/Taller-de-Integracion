import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../modules/auth/hook/auth.hook';
import { ReactNode } from 'react';

/**
 * PrivateRoute component that ensures only authenticated users can access the route.
 * If the user is not authenticated, it redirects to the login page.
 */
interface PrivateRouteProps {
  children?: ReactNode;
}

const PrivateRoute: React.FC<PrivateRouteProps> = ({ children }) => {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" />;
  }

  return <>{children || <Outlet />}</>;
};

export default PrivateRoute;
