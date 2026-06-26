# LiveSync

<p align="center">
  <strong>Multi-tenant real-time sync platform</strong><br/>
  Database-per-tenant SaaS · CQRS · SignalR · React · Prometheus
</p>

<p align="center">
  <a href="https://github.com/ismayilov449/LiveSync/actions/workflows/ci.yml"><img src="https://github.com/ismayilov449/LiveSync/actions/workflows/ci.yml/badge.svg" alt="CI"/></a>
</p>

---

## 📖 Full documentation

**All project documentation lives in [`LiveSync.PushPlatform/README.md`](LiveSync.PushPlatform/README.md).**

Includes solution architecture (C4, NFRs, ADRs), tenant admin console, observability stack, demo walkthrough, and **resume-ready bullets** for solution architect roles.

---

## ⚡ Quick start

```bash
cd LiveSync.PushPlatform
docker compose up -d
cd LiveSync.API/client && npm install && npm run build && cd ../..
dotnet run --project LiveSync.API
dotnet run --project LiveSync.Worker
```

Open http://localhost:5252 — login: `admin@livesync.local` / `Admin123!`

**Tenant admins:** use **Admin** in the header for users, audit, queue stats, and suspend/reactivate.

---

## Repository layout

```
LiveSync/
├── LiveSync.PushPlatform/     ← Main solution (read README inside)
│   ├── LiveSync.API/          ← REST + React SPA + SignalR + /metrics
│   ├── LiveSync.Worker/       ← Change queue processor + /metrics
│   ├── LiveSync.Domain/
│   ├── LiveSync.Application/
│   ├── LiveSync.Infrastructure/
│   ├── docs/                  ← Architecture, walkthrough, ADRs
│   ├── observability/         ← Prometheus, Grafana, OTLP (optional)
│   └── docker-compose.yml
└── .github/workflows/         ← CI
```

---

## License

MIT — see [LiveSync.PushPlatform/LICENSE](LiveSync.PushPlatform/LICENSE).
