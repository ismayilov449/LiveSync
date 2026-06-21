import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function AppLayout() {
  const { session, logout, isTenantAdmin } = useAuth();

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand">
          <span className="brand-mark">LS</span>
          <div>
            <strong>LiveSync</strong>
            <span className="muted">Tenant {session?.tenantId}</span>
          </div>
        </div>
        <nav className="app-nav">
          <NavLink to="/items" className={({ isActive }) => isActive ? 'active' : ''}>
            Items
          </NavLink>
          <NavLink to="/profile" className={({ isActive }) => isActive ? 'active' : ''}>
            Profile
          </NavLink>
          {isTenantAdmin && (
            <NavLink to="/profile#invite" className={({ isActive }) => isActive ? 'active' : ''}>
              Invite
            </NavLink>
          )}
          <NavLink to="/about" className={({ isActive }) => isActive ? 'active' : ''}>
            About
          </NavLink>
          <button type="button" className="btn btn-ghost" onClick={logout}>
            Sign out
          </button>
        </nav>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
}
