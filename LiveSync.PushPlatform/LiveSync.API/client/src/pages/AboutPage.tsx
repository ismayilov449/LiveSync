export function AboutPage() {
  return (
    <div className="panel about-page">
      <div className="panel-header">
        <h2>About LiveSync</h2>
        <p className="muted">Portfolio project — multi-tenant real-time sync platform</p>
      </div>

      <section className="about-section">
        <h3>What it demonstrates</h3>
        <ul>
          <li>Database-per-tenant isolation with a central control plane</li>
          <li>CQRS + MediatR, FluentValidation, domain events</li>
          <li>Outbox-style change queue processed by a background worker</li>
          <li>Redis-backed subscriptions and SignalR live push</li>
          <li>ASP.NET Identity, JWT auth, tenant-scoped RBAC</li>
        </ul>
      </section>

      <section className="about-section">
        <h3>Architecture</h3>
        <pre className="architecture-diagram">{`┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  React SPA  │────▶│  LiveSync    │────▶│ Control     │
│  + SignalR  │     │  API         │     │ Plane DB    │
└─────────────┘     └──────┬───────┘     └─────────────┘
                           │
                    ┌──────▼───────┐     ┌─────────────┐
                    │ Change Queue │────▶│ LiveSync    │
                    │ (tenant DB)  │     │ Worker      │
                    └──────────────┘     └──────┬──────┘
                           │                    │
                    ┌──────▼───────┐     ┌──────▼──────┐
                    │ Tenant DBs   │     │ Redis +     │
                    │ (per tenant) │     │ SignalR push│
                    └──────────────┘     └─────────────┘`}</pre>
      </section>

      <section className="about-section">
        <h3>Documentation</h3>
        <p>
          See the repository <code>docs/</code> folder for architecture notes and ADRs.
        </p>
      </section>
    </div>
  );
}
