import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function AdminRoute() {
  const { isAuthenticated, isTenantAdmin } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!isTenantAdmin) {
    return <Navigate to="/items" replace />;
  }

  return <Outlet />;
}
