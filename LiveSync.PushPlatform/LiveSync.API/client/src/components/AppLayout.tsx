import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function AppLayout() {
  const { session, logout, isTenantAdmin } = useAuth();
  const location = useLocation();

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand">
          <span className="brand-wordmark">livesync</span>
          <span className="brand-meta">t/{session?.tenantId}</span>
        </div>
        <nav className="app-nav">
          <NavLink to="/items" className={({ isActive }) => (isActive ? 'active' : '')}>
            Items
          </NavLink>
          {isTenantAdmin && (
            <NavLink
              to="/admin/overview"
              className={() => (location.pathname.startsWith('/admin') ? 'active' : '')}
            >
              Admin
            </NavLink>
          )}
          <NavLink to="/profile" className={({ isActive }) => (isActive ? 'active' : '')}>
            Account
          </NavLink>
          <NavLink to="/about" className={({ isActive }) => (isActive ? 'active' : '')}>
            About
          </NavLink>
          <button type="button" className="btn btn-ghost btn-sm" onClick={logout}>
            Sign out
          </button>
        </nav>
      </header>
      <main className="app-main app-main-wide">
        <Outlet />
      </main>
    </div>
  );
}
