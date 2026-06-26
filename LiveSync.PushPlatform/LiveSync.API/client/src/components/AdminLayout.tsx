import { NavLink, Outlet } from 'react-router-dom';
import { useProfile } from '../hooks/useProfile';
import { TenantStatusChip } from './TenantStatusChip';

const adminLinks = [
  { to: '/admin/overview', label: 'Overview' },
  { to: '/admin/users', label: 'Users' },
  { to: '/admin/audit', label: 'Audit' },
  { to: '/admin/settings', label: 'Settings' },
] as const;

export function AdminLayout() {
  const { profile, loading } = useProfile();

  return (
    <div className="admin-layout">
      <aside className="admin-sidebar">
        <div className="admin-sidebar-header">
          <h2>Admin</h2>
          {loading ? (
            <p className="muted">…</p>
          ) : profile ? (
            <>
              <p className="admin-tenant-name">{profile.tenantName || `Tenant ${profile.tenantId}`}</p>
              <TenantStatusChip status={profile.tenantStatus} />
            </>
          ) : null}
        </div>
        <nav className="admin-nav">
          {adminLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) => (isActive ? 'active' : '')}
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
        <p className="muted admin-sidebar-foot">
          Users, audit trail, lifecycle.
        </p>
      </aside>
      <div className="admin-content">
        <Outlet context={profile} />
      </div>
    </div>
  );
}
