export function AboutPage() {
  return (
    <div className="panel about-page">
      <div className="panel-header">
        <h2>About LiveSync</h2>
        <p className="muted">
          Portfolio project — multi-tenant <strong>support desk</strong> with real-time sync
        </p>
      </div>

      <section className="about-section">
        <h3>What it is</h3>
        <p>
          LiveSync is a B2B-style SaaS sample: each organization (tenant) gets an isolated SQL
          database. Agents collaborate on <strong>tickets</strong> organized into{' '}
          <strong>queues</strong>, with live updates when anyone opens, comments, assigns, or
          changes status.
        </p>
      </section>

      <section className="about-section">
        <h3>Support desk domain</h3>
        <ul>
          <li>
            <strong>Queue</strong> — work streams per tenant (e.g. General, IT)
          </li>
          <li>
            <strong>Ticket</strong> — aggregate with status lifecycle: New → Assigned → In
            progress → Resolved → Closed
          </li>
          <li>
            <strong>Comments</strong> — entities inside the ticket aggregate (not a separate
            resource)
          </li>
          <li>
            <strong>Assign</strong> — tenant admins pick agents from the tenant user list
          </li>
          <li>
            <strong>Rules</strong> — e.g. cannot deactivate a queue while open tickets exist
          </li>
        </ul>
      </section>

      <section className="about-section">
        <h3>What it demonstrates</h3>
        <ul>
          <li>Database-per-tenant isolation with a central control plane</li>
          <li>DDD aggregates, CQRS + MediatR, FluentValidation, domain events</li>
          <li>Outbox-style change queue processed by a background worker (dead-letter path)</li>
          <li>
            Bucket-scoped SignalR push — Tickets page ignores Queue changes and vice versa
          </li>
          <li>Row-level client patch + remote-change flash on collaborative edits</li>
          <li>ASP.NET Identity, JWT auth, tenant-scoped RBAC</li>
          <li>Tenant admin console — users, audit, change-queue health, suspend/reactivate</li>
          <li>Prometheus metrics, health probes, optional OTLP export</li>
        </ul>
      </section>

      <section className="about-section">
        <h3>Architecture</h3>
        <pre className="architecture-diagram">{`┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  React SPA  │────▶│  LiveSync    │────▶│ Control     │
│ Tickets +   │     │  API         │     │ Plane DB    │
│ Queues +    │     │  REST + JWT  │     │ users/audit │
│ SignalR     │     │  + SignalR   │     └─────────────┘
└─────────────┘     └──────┬───────┘
                           │
                    ┌──────▼───────┐     ┌─────────────┐
                    │ Change Queue │────▶│ LiveSync    │
                    │ (tenant DB)  │     │ Worker      │
                    └──────────────┘     └──────┬──────┘
                           │                    │
                    ┌──────▼───────┐     ┌──────▼──────┐
                    │ Tenant DBs   │     │ Redis +     │
                    │ queues       │     │ SignalR     │
                    │ tickets      │     │ backplane   │
                    │ comments     │     └─────────────┘
                    └──────────────┘`}</pre>
        <p className="muted form-hint">
          Two push paths: API notifies SignalR immediately; the worker polls the outbox for Redis
          cache consistency (~1s).
        </p>
      </section>

      <section className="about-section">
        <h3>Using this app</h3>
        <ul>
          <li>
            <strong>Tickets</strong> — open tickets, add comments, advance workflow (assign is
            admin-only)
          </li>
          <li>
            <strong>Queues</strong> — manage work streams; admins can deactivate/delete when safe
          </li>
          <li>
            <strong>Admin</strong> — invite users, view audit log and queue stats, suspend tenant
          </li>
        </ul>
      </section>

      <section className="about-section">
        <h3>Documentation</h3>
        <p>
          See the repository <code>docs/</code> folder — start with{' '}
          <code>demo-walkthrough.md</code>, <code>solution-architecture.md</code>, and ADRs{' '}
          <code>001</code>–<code>006</code> (including Support Desk aggregates in ADR 006).
        </p>
      </section>
    </div>
  );
}
