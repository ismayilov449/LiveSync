# Documentation assets

Visual demos for the GitHub README and technical docs. All files below are checked into this folder.

## Files

| File | Purpose |
|------|---------|
| `screenshot-tickets.png` | Tickets page — **signalr · live** status, list + detail panel |
| `screenshot-queues.png` | Queues page — bucket-scoped live status |
| `demo-realtime-sync.gif` | Two tabs (admin + member), live ticket updates without refresh |
| `demo-ticket-workflow.gif` | Assign → start progress → resolve → close |
| `demo-tenant-isolation.gif` | Two tenants — separate ticket universes |
| `demo-admin-console.png` | Admin → Overview — queue pending / dead-letter stats |

## Regenerate (automated)

With Docker, API, and Worker running:

```bash
pip install playwright requests Pillow
python -m playwright install chromium
python scripts/capture-demo-assets.py
```

The script uses the dev token endpoint (`POST /dev/auth/token`) and Playwright headless Chrome — no manual ScreenToGif session required.

## Regenerate (manual)

Follow [demo-walkthrough.md](../demo-walkthrough.md) Scenarios 2–4 and use [ScreenToGif](https://www.screentogif.com/) if you prefer hand-recorded GIFs.

### UI notes (current design)

The SPA uses a **dark, compact, monospace-accented** theme:

- Header: `livesync` wordmark + `t/{tenantId}`
- Nav: **Tickets**, **Queues**, **Admin** (admin only), **Account**
- Live status: green dot + `signalr · live`
- Ticket detail: status pill, workflow buttons, assign dropdown, comments
- Admin sidebar: Overview, Users, Audit, Settings

Capture screenshots with the API running and Worker started so queue stats are populated on **Admin → Overview**.

### Recording tips

- **Resolution:** 1280×720 or 1920×1080
- **Length:** 15–30 seconds per GIF
- **Size:** Keep under 10 MB for GitHub (use ScreenToGif optimizer or the capture script)
