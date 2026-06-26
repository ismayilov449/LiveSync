# Documentation assets

Place visual assets here for the GitHub README.

## Recommended files

| File | Purpose | How to create |
|------|---------|----------------|
| `demo-realtime-sync.gif` | Two tabs, both users see live ticket updates | [demo-walkthrough.md](../demo-walkthrough.md) Scenario 2 + [ScreenToGif](https://www.screentogif.com/) |
| `demo-ticket-workflow.gif` | Assign → start → resolve → close | Record Scenario 3 from walkthrough |
| `demo-tenant-isolation.gif` | Two tenants, separate data | Record Scenario 4 from walkthrough |
| `demo-admin-console.png` | Admin overview with queue stats | Screenshot **Admin → Overview** while Worker is running |
| `screenshot-tickets.png` | Static hero image | Screenshot of Tickets page with **signalr · live** status and detail panel |
| `screenshot-queues.png` | Queues page | Screenshot with bucket-scoped live status |

## UI notes (current design)

The SPA uses a **dark, compact, monospace-accented** theme:

- Header: `livesync` wordmark + `t/{tenantId}`
- Nav: **Tickets**, **Queues**, **Admin** (admin only), **Account**
- Live status: green dot + `signalr · live`
- Ticket detail: status pill, workflow buttons, assign dropdown, comments
- Admin sidebar: Overview, Users, Audit, Settings

Capture screenshots with the API running and Worker started so queue stats are populated.

## Recording tips

- **Resolution:** 1280×720 or 1920×1080
- **Length:** 15–30 seconds per GIF
- **Size:** Keep under 10 MB for GitHub (use ScreenToGif optimizer)
- **Focus:** Show the ticket appearing in the *other* tab without refresh, or a comment syncing live

## After adding GIFs

Uncomment or add image lines in the root `README.md` **Live demo** section.
