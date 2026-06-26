# Troubleshooting

Common local development issues and fixes. For scripted demos see [demo-walkthrough.md](demo-walkthrough.md).

## Quick checks

```bash
docker compose ps                    # SQL Server + Redis running?
cd LiveSync.API/client && npm run build   # wwwroot populated?
dotnet run --project LiveSync.API    # API on :5252
dotnet run --project LiveSync.Worker # Worker on :5260 metrics
```

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Blank page / 404 on `/` | `wwwroot/` empty (not committed) | `cd LiveSync.API/client && npm ci && npm run build` |
| Login fails / DB errors | SQL not running or wrong connection string | `docker compose up -d`; use Docker config (see [Configuration](#configuration)) |
| Migration errors after pull | Schema changed (Support Desk migration) | Run API with `Hosting:ApplyMigrationsOnStartup` or `dotnet ef database update`; or recreate Docker volumes |
| **signalr · offline** | API down or stale tab | Start API; hard-refresh (Ctrl+Shift+R) |
| No live updates | Worker or Redis down | `docker compose up -d`; start Worker |
| Ticket change refreshes Queues tab | Old client build | Rebuild client; bucket-scoped push requires current SPA |
| Assign dropdown empty | No users in tenant | **Admin → Users** to invite agents; or only admin exists |
| Cannot start progress | Ticket not assigned | Tenant admin must **Assign** first |
| Queue deactivate fails (409) | Open tickets in queue | Resolve/close tickets or move them first |
| Queue stats show `—` | Worker not running | `dotnet run --project LiveSync.Worker` |
| Port 5252 in use | Previous API instance | Stop old process or change `launchSettings.json` |
| Integration tests fail | Docker Desktop not running | Start Docker; tests use Testcontainers |
| Prometheus targets down | API/Worker not on host ports | On Docker Desktop use `host.docker.internal` in scrape config |
| Admin nav missing | User lacks `TenantAdmin` | Login as admin or invite with admin role |
| JWT / auth errors on `dotnet run` | Empty `SecretKey` without Development env | Set `ASPNETCORE_ENVIRONMENT=Development` or use User Secrets |
| `npm ci` ERESOLVE (vite vs plugin-react) | Vite 8 with `@vitejs/plugin-react` 4.x | Use Vite 6 + plugin-react 4 (see `client/package.json`); commit `package-lock.json` |
| Windows SQL vs Docker mismatch | `appsettings.Development.json` uses Trusted_Connection | Use Docker SQL auth or copy [appsettings.Development.docker.example.json](../LiveSync.API/appsettings.Development.docker.example.json) |

## Configuration

### Docker (recommended)

`appsettings.json` defaults match `docker-compose.yml`:

- Server: `localhost,1433`
- User: `sa` / `Your_password123`

Run API with Development environment for JWT secret:

```bash
# PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project LiveSync.API
```

Or copy `LiveSync.API/appsettings.Development.docker.example.json` → `appsettings.Development.json`.

### Windows native SQL Server

Use Trusted Connection in `appsettings.Development.json` (already provided). Ensure local SQL has `LiveSync_ControlPlane` and tenant DBs migrated.

### JWT secret

```bash
dotnet user-secrets set "Auth:Jwt:SecretKey" "local-dev-signing-key-change-me-min-32-chars" --project LiveSync.API
```

## Scripts

From repo root:

```powershell
.\scripts\dev.ps1    # Windows: Docker + client build + start API & Worker
```

```bash
./scripts/dev.sh     # Linux/macOS
```

## Still stuck?

1. Check API logs in the terminal running `dotnet run`
2. Open http://localhost:5252/health/ready
3. Scalar OpenAPI: http://localhost:5252/scalar/v1 (Development)
4. See [CONTRIBUTING.md](../CONTRIBUTING.md) and [client-development.md](client-development.md)
