# LiveSync

<p align="center">
  <strong>Multi-tenant real-time sync platform</strong><br/>
  Database-per-tenant SaaS · CQRS · SignalR · React
</p>

<p align="center">
  <a href="https://github.com/ismayilov449/LiveSync/actions/workflows/ci.yml"><img src="https://github.com/ismayilov449/LiveSync/actions/workflows/ci.yml/badge.svg" alt="CI"/></a>
</p>

---

## 📖 Full documentation

**All project documentation lives in [`LiveSync.PushPlatform/README.md`](LiveSync.PushPlatform/README.md).**

That README includes:

- Architecture diagrams (Mermaid + visuals)
- Step-by-step demo walkthrough for reviewers
- Multi-tenancy, RBAC, and real-time sync explained
- Quick start, API reference, testing, Docker

**Start here if you're evaluating this repo:**

1. [LiveSync.PushPlatform/README.md](LiveSync.PushPlatform/README.md) — main guide
2. [docs/demo-walkthrough.md](LiveSync.PushPlatform/docs/demo-walkthrough.md) — hands-on 10-minute demo
3. [docs/real-time-sync.md](LiveSync.PushPlatform/docs/real-time-sync.md) — how live push works

---

## ⚡ Quick start

```bash
cd LiveSync.PushPlatform
docker compose up -d
cd LiveSync.API/client && npm install && npm run build && cd ../..
dotnet run --project LiveSync.PushPlatform/LiveSync.API
dotnet run --project LiveSync.PushPlatform/LiveSync.Worker
```

Open http://localhost:5252 — login: `admin@livesync.local` / `Admin123!`

---

## Repository layout

```
LiveSync/
├── LiveSync.PushPlatform/     ← Main solution (read README inside)
│   ├── LiveSync.API/          ← REST + React SPA + SignalR
│   ├── LiveSync.Worker/       ← Change queue processor
│   ├── LiveSync.Domain/
│   ├── LiveSync.Application/
│   ├── LiveSync.Infrastructure/
│   ├── docs/                  ← Architecture, walkthrough, ADRs
│   └── docker-compose.yml
└── .github/workflows/         ← CI
```

---

## License

MIT — see [LiveSync.PushPlatform/LICENSE](LiveSync.PushPlatform/LICENSE).
