import { Navigate, Route, Routes } from 'react-router-dom';

import { AdminRoute } from './auth/AdminRoute';

import { AuthProvider } from './auth/AuthContext';

import { GuestRoute, ProtectedRoute } from './auth/ProtectedRoute';

import { AdminLayout } from './components/AdminLayout';

import { AppLayout } from './components/AppLayout';

import { AboutPage } from './pages/AboutPage';

import { AdminAuditPage } from './pages/admin/AdminAuditPage';

import { AdminOverviewPage } from './pages/admin/AdminOverviewPage';

import { AdminSettingsPage } from './pages/admin/AdminSettingsPage';

import { AdminUsersPage } from './pages/admin/AdminUsersPage';

import { LoginPage } from './pages/LoginPage';

import { ProfilePage } from './pages/ProfilePage';

import { QueuesPage } from './pages/QueuesPage';

import { RegisterPage } from './pages/RegisterPage';

import { TicketsPage } from './pages/TicketsPage';

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route element={<GuestRoute />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>

        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/tickets" element={<TicketsPage />} />
            <Route path="/queues" element={<QueuesPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/about" element={<AboutPage />} />

            <Route element={<AdminRoute />}>
              <Route path="/admin" element={<AdminLayout />}>
                <Route index element={<Navigate to="overview" replace />} />
                <Route path="overview" element={<AdminOverviewPage />} />
                <Route path="users" element={<AdminUsersPage />} />
                <Route path="audit" element={<AdminAuditPage />} />
                <Route path="settings" element={<AdminSettingsPage />} />
              </Route>
            </Route>
          </Route>
        </Route>

        <Route path="/" element={<Navigate to="/tickets" replace />} />
        <Route path="*" element={<Navigate to="/tickets" replace />} />
      </Routes>
    </AuthProvider>
  );
}
