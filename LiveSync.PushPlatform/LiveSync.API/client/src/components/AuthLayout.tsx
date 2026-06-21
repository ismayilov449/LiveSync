import type { FormEvent, ReactNode } from 'react';
import { Link } from 'react-router-dom';

interface AuthLayoutProps {
  title: string;
  subtitle: string;
  footer: ReactNode;
  children: ReactNode;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
}

export function AuthLayout({ title, subtitle, footer, children, onSubmit }: AuthLayoutProps) {
  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <span className="brand-mark lg">LS</span>
          <h1>{title}</h1>
          <p className="muted">{subtitle}</p>
        </div>
        <form className="stack" onSubmit={onSubmit}>
          {children}
        </form>
        <div className="auth-footer muted">{footer}</div>
      </div>
    </div>
  );
}

export function AuthLink({ to, children }: { to: string; children: ReactNode }) {
  return <Link to={to}>{children}</Link>;
}
