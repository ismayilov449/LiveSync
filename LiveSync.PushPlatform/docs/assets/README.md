# Documentation assets

Demo media for the repository README and docs. Everything here is recorded from the running app by [`scripts/capture-demo-assets.py`](../../scripts/capture-demo-assets.py); nothing is drawn or mocked up by hand.

## Files

| File | Shows |
|---|---|
| `demo-realtime-sync.gif` | Two browsers in one tenant: Sarah (admin) opens and assigns a ticket; Marcus's list updates in place |
| `demo-ticket-workflow.gif` | An agent moves a ticket Assigned → In progress → Resolved → Closed and adds a comment |
| `demo-tenant-isolation.gif` | A new organization registers live; Acme's ticket never reaches Globex |
| `screenshot-tickets.png` | Tickets page with the live status pill, list and detail panel with comments |
| `screenshot-queues.png` | Queues page |
| `screenshot-admin-overview.png` | Admin → Overview: organization status and outbox pending / dead-letter counts |
| `screenshot-admin-audit.png` | Admin → Audit log |

## Regenerate

Start the stack against a **fresh database** (the script seeds the demo organization "Acme Inc." through the public API and expects an empty tenant):

```bash
docker compose up -d
dotnet run --project LiveSync.API        # Development environment, http://localhost:5252
dotnet run --project LiveSync.Worker

pip install playwright pillow requests imageio-ffmpeg
python -m playwright install chromium
python scripts/capture-demo-assets.py                 # everything
python scripts/capture-demo-assets.py realtime        # or: workflow, isolation, screenshots
```

### How the recordings are made

- Each person is a separate Playwright browser context, so sessions are as isolated as two real browsers.
- Stacked panes are recorded at **1000×440 CSS px with a 1.5× device scale**, so each pane displays near 1:1 at GitHub's README width. Screenshots are taken at **2×**.
- A cursor overlay and click ripple are injected so viewers can follow the actions; captions are composed with Pillow.
- Frames are captured continuously with real timing, identical frames are merged, and ffmpeg encodes the GIFs with an optimized 256-colour palette.
- The script waits for the Worker to drain the change queue after seeding, so only the demo's own changes are flagged as remote.
