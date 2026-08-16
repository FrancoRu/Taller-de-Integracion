import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { ReactNode } from 'react';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

/**
 * PrivateRoute component that ensures only authenticated users can access the route.
 * If the user is not authenticated, it redirects to the login page.
 */
interface PrivateRouteProps {
  children?: ReactNode;
  allowedRoles?: UserRolesType[];
}

const PrivateRoute: React.FC<PrivateRouteProps> = ({
  children,
  allowedRoles,
}) => {
  const { isAuthenticated, role } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to={APP_ROUTES.login} replace />;
  }

  if (allowedRoles && !allowedRoles.includes(role)) {
    return <Navigate to={APP_ROUTES.forbidden} replace />;
  }

  return <>{children || <Outlet />}</>;
};

export default PrivateRoute;
