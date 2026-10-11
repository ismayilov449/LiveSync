#!/usr/bin/env python3
"""Record the README demo GIFs and screenshots from a running LiveSync dev stack.

Prerequisites (run against a FRESH database for clean recordings):
    docker compose up -d                      # SQL Server + Redis
    dotnet run --project LiveSync.API         # http://localhost:5252 (Development)
    dotnet run --project LiveSync.Worker

    pip install playwright pillow requests imageio-ffmpeg
    python -m playwright install chromium
    python scripts/capture-demo-assets.py

The script seeds a demo organization ("Acme Inc.") through the public REST API,
drives real browsers with Playwright (two isolated sessions where needed), and
encodes GIFs with an ffmpeg palette so the dark UI stays sharp.
"""

from __future__ import annotations

import asyncio
import math
import shutil
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass, field
from io import BytesIO
from pathlib import Path

import imageio_ffmpeg
import requests
from PIL import Image, ImageDraw, ImageFont
from playwright.async_api import Browser, Locator, Page, async_playwright

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "docs" / "assets"
BASE_URL = "http://localhost:5252"
PASSWORD = "Demo1234!"

GIF_SCALE = 1.5          # device scale factor for GIF frames
SHOT_SCALE = 2           # device scale factor for static screenshots
PANE = (1000, 440)       # CSS viewport of each stacked browser pane

BG = (10, 10, 12)
BAR = (22, 22, 27)
TEXT = (236, 237, 240)
MUTED = (150, 154, 163)
ACCENT = (52, 211, 153)
ADMIN = (129, 140, 248)
AGENT = (251, 191, 36)

# ── REST helpers ────────────────────────────────────────────────────────────


def api(method: str, path: str, token: str | None = None, **kwargs):
    headers = {"Authorization": f"Bearer {token}"} if token else {}
    response = requests.request(method, f"{BASE_URL}/api/v1{path}", headers=headers, timeout=30, **kwargs)
    response.raise_for_status()
    return response.json() if response.content else None


def login(user: str) -> dict | None:
    response = requests.post(
        f"{BASE_URL}/api/v1/auth/login", json={"userName": user, "password": PASSWORD}, timeout=30)
    return response.json() if response.ok else None


def login_or_register(org: str, user: str, display_name: str) -> dict:
    return login(user) or api("POST", "/auth/register", json={
        "tenantName": org,
        "userName": user,
        "email": f"{user}@{org.split()[0].lower()}.test",
        "password": PASSWORD,
        "displayName": display_name,
    })


@dataclass
class DemoOrg:
    admin: dict
    agent: dict
    queues: dict[str, int]
    tickets: dict[str, int] = field(default_factory=dict)


BACKLOG = [
    # subject, queue, priority (0 low, 1 normal, 2 high), workflow steps
    ("Add new hire: Priya Patel", "Onboarding", 1, ["assign", "start", "resolve", "close"]),
    ("Invoice #4821 charged twice", "Billing", 2, ["assign", "start", "resolve"]),
    ("SSO login fails for contractors", "IT Support", 2, ["assign", "start", "comment"]),
    ("VPN disconnects every 30 minutes", "IT Support", 1, ["assign"]),
    ("Export to CSV times out on large reports", "General", 0, []),
    ("Update company billing address", "Billing", 0, []),
]


def seed_demo_org() -> DemoOrg:
    admin = login_or_register("Acme Inc.", "sarah", "Sarah Chen")
    token = admin["accessToken"]

    if not login("marcus"):
        api("POST", "/auth/users", token, json={
            "userName": "marcus", "email": "marcus@acme.test", "password": PASSWORD, "displayName": "Marcus Lee"})
    agent = login("marcus")

    queues = {q["name"]: q["id"] for q in api("GET", "/queues", token)["items"]}
    for name in ["Billing", "IT Support", "Onboarding"]:
        if name not in queues:
            queues[name] = api("POST", "/queues", token, json={"name": name})

    org = DemoOrg(admin, agent, queues)
    existing = api("GET", "/tickets?pageSize=50", token)["items"]
    org.tickets = {t["subject"]: t["id"] for t in existing}
    if existing:
        return org

    for subject, queue, priority, steps in BACKLOG:
        ticket_id = api("POST", "/tickets", token, json={
            "queueId": queues[queue], "subject": subject, "description": subject,
            "priority": priority, "reporterUserId": admin["userId"]})
        org.tickets[subject] = ticket_id
        for step in steps:
            if step == "assign":
                api("PUT", f"/tickets/{ticket_id}/assign", token, json={"assigneeUserId": agent["userId"]})
            elif step == "start":
                api("POST", f"/tickets/{ticket_id}/start-progress", agent["accessToken"])
            elif step == "resolve":
                api("POST", f"/tickets/{ticket_id}/resolve", agent["accessToken"])
            elif step == "close":
                api("POST", f"/tickets/{ticket_id}/close", token)
            elif step == "comment":
                api("POST", f"/tickets/{ticket_id}/comments", agent["accessToken"], json={
                    "authorUserId": agent["userId"],
                    "body": "Reproduced: the IdP rejects contractor accounts without an employee ID."})
                api("POST", f"/tickets/{ticket_id}/comments", token, json={
                    "authorUserId": admin["userId"],
                    "body": "Thanks — I'll ask HR to backfill IDs for the 12 affected contractors."})
    wait_for_worker(token)
    return org


def wait_for_worker(token: str, timeout: float = 60) -> None:
    """Let the Worker drain the seed changes so their pushes don't flash rows during recording."""
    deadline = time.monotonic() + timeout
    while api("GET", "/operations/change-queue", token)["pendingCount"] > 0:
        if time.monotonic() > deadline:
            raise SystemExit("Change queue is not draining — is LiveSync.Worker running?")
        time.sleep(0.5)
    time.sleep(2)


# ── Browser helpers ─────────────────────────────────────────────────────────

CURSOR_SCRIPT = """
(() => {
  if (window.__demoCursor) return;
  window.__demoCursor = true;
  const install = () => {
    const cursor = document.createElement('div');
    cursor.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24"><path d="M4 2 L4 19 L8.5 14.8 L11.6 21.5 L14.4 20.3 L11.3 13.7 L17.5 13.4 Z" fill="#fff" stroke="#111" stroke-width="1.4" stroke-linejoin="round"/></svg>';
    Object.assign(cursor.style, { position: 'fixed', left: '-40px', top: '-40px', zIndex: 2147483647, pointerEvents: 'none' });
    document.documentElement.appendChild(cursor);
    document.addEventListener('mousemove', e => {
      cursor.style.left = (e.clientX - 3) + 'px';
      cursor.style.top = (e.clientY - 2) + 'px';
    }, true);
    document.addEventListener('mousedown', e => {
      const ring = document.createElement('div');
      Object.assign(ring.style, {
        position: 'fixed', left: (e.clientX - 14) + 'px', top: (e.clientY - 14) + 'px',
        width: '28px', height: '28px', borderRadius: '50%', border: '2px solid #34d399',
        zIndex: 2147483646, pointerEvents: 'none', transition: 'transform .45s ease-out, opacity .45s ease-out'
      });
      document.documentElement.appendChild(ring);
      requestAnimationFrame(() => { ring.style.transform = 'scale(1.9)'; ring.style.opacity = '0'; });
      setTimeout(() => ring.remove(), 500);
    }, true);
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', install); else install();
})();
"""


class Actor:
    """One signed-in browser session; its cursor appears once the actor first moves."""

    def __init__(self, page: Page):
        self.page = page
        self.x, self.y = PANE[0] * 0.5, PANE[1] * 0.8

    @classmethod
    async def open(cls, browser: Browser, session: dict | None, scale: float, viewport=PANE,
                   cursor: bool = True) -> "Actor":
        context = await browser.new_context(viewport={"width": viewport[0], "height": viewport[1]},
                                            device_scale_factor=scale)
        if cursor:
            await context.add_init_script(CURSOR_SCRIPT)
        page = await context.new_page()
        if session:
            await page.goto(f"{BASE_URL}/login")
            await page.evaluate("s => localStorage.setItem('livesync.auth', JSON.stringify(s))", session)
        actor = cls(page)
        return actor

    async def goto_live(self, path: str) -> None:
        await self.page.goto(f"{BASE_URL}{path}")
        await self.page.get_by_text("signalr · live").first.wait_for(timeout=30_000)

    async def move(self, target: Locator, duration: float = 0.55) -> tuple[float, float]:
        await target.scroll_into_view_if_needed()
        box = await target.bounding_box()
        tx, ty = box["x"] + min(box["width"] / 2, 60), box["y"] + box["height"] / 2
        steps = max(8, int(duration * 45))
        sx, sy = self.x, self.y
        for i in range(1, steps + 1):
            t = i / steps
            ease = 0.5 - math.cos(math.pi * t) / 2
            self.x, self.y = sx + (tx - sx) * ease, sy + (ty - sy) * ease
            await self.page.mouse.move(self.x, self.y)
            await asyncio.sleep(duration / steps)
        return tx, ty

    async def click(self, target: Locator, pause: float = 0.25) -> None:
        await self.move(target)
        await asyncio.sleep(0.12)
        await self.page.mouse.down()
        await asyncio.sleep(0.06)
        await self.page.mouse.up()
        await asyncio.sleep(pause)

    async def type(self, target: Locator, text: str) -> None:
        await self.click(target, pause=0.1)
        await target.press_sequentially(text, delay=55)

    async def select(self, target: Locator, label: str) -> None:
        await self.click(target, pause=0.15)
        await target.select_option(label=label)
        await asyncio.sleep(0.35)


# ── Frame recording & composition ───────────────────────────────────────────


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = (["segoeuib.ttf", "DejaVuSans-Bold.ttf", "Arial Bold.ttf"] if bold
                  else ["segoeui.ttf", "DejaVuSans.ttf", "Arial.ttf"])
    for name in candidates:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


@dataclass
class Pane:
    actor: Actor
    title: str
    subtitle: str
    color: tuple[int, int, int]


class Recorder:
    """Screenshots every pane continuously while a scene plays; captions follow the scene."""

    def __init__(self, panes: list[Pane]):
        self.panes = panes
        self.caption = ""
        self.raw: list[tuple[float, list[bytes], str, list[str]]] = []
        self._running = False

    async def __aenter__(self) -> "Recorder":
        self._running = True
        self._task = asyncio.create_task(self._loop())
        return self

    async def __aexit__(self, *exc) -> None:
        await asyncio.sleep(0.4)
        self._running = False
        await self._task

    async def hold(self, seconds: float) -> None:
        await asyncio.sleep(seconds)

    async def _loop(self) -> None:
        while self._running:
            started = time.monotonic()
            shots = [await pane.actor.page.screenshot(type="png") for pane in self.panes]
            self.raw.append((started, shots, self.caption, [pane.subtitle for pane in self.panes]))
            await asyncio.sleep(max(0.0, 0.1 - (time.monotonic() - started)))

    def compose(self, shots: list[bytes], caption: str, subtitles: list[str]) -> Image.Image:
        s = GIF_SCALE
        bar_h, gap, cap_h = int(38 * s), int(10 * s), int(54 * s)
        images = [Image.open(BytesIO(shot)).convert("RGB") for shot in shots]
        width = images[0].width
        height = sum(bar_h + im.height for im in images) + gap * (len(images) - 1) + (cap_h if caption else 0)
        canvas = Image.new("RGB", (width, height), BG)
        draw = ImageDraw.Draw(canvas)
        title_font, sub_font, cap_font = font(int(15 * s), bold=True), font(int(13 * s)), font(int(17 * s))

        y = 0
        for pane, subtitle, im in zip(self.panes, subtitles, images):
            draw.rectangle((0, y, width, y + bar_h), fill=BAR)
            cy = y + bar_h // 2
            r = int(5 * s)
            draw.ellipse((int(14 * s), cy - r, int(14 * s) + 2 * r, cy + r), fill=pane.color)
            draw.text((int(30 * s), cy), pane.title, font=title_font, fill=TEXT, anchor="lm")
            draw.text((width - int(16 * s), cy), subtitle, font=sub_font, fill=MUTED, anchor="rm")
            canvas.paste(im, (0, y + bar_h))
            y += bar_h + im.height + gap
        if caption:
            y -= gap
            draw.rectangle((0, y, width, height), fill=BAR)
            draw.text((width // 2, y + cap_h // 2), caption, font=cap_font, fill=TEXT, anchor="mm")
        return canvas

    def save_gif(self, name: str, final_hold: float = 2.8) -> None:
        frames: list[tuple[Image.Image, float]] = []
        for i, (started, shots, caption, subtitles) in enumerate(self.raw):
            ends = self.raw[i + 1][0] if i + 1 < len(self.raw) else started + final_hold
            image = self.compose(shots, caption, subtitles)
            if frames and frames[-1][0].tobytes() == image.tobytes():
                frames[-1] = (frames[-1][0], frames[-1][1] + ends - started)
            else:
                frames.append((image, ends - started))
        encode_gif(frames, ASSETS / name)


def encode_gif(frames: list[tuple[Image.Image, float]], path: Path) -> None:
    workdir = Path(tempfile.mkdtemp(prefix="livesync-gif-"))
    try:
        lines = []
        for i, (image, duration) in enumerate(frames):
            file = workdir / f"f{i:04d}.png"
            image.save(file)
            lines += [f"file '{file.as_posix()}'", f"duration {max(duration, 0.02):.3f}"]
        lines.append(f"file '{(workdir / f'f{len(frames) - 1:04d}.png').as_posix()}'")
        (workdir / "frames.txt").write_text("\n".join(lines), encoding="utf-8")
        subprocess.run([
            imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-loglevel", "error",
            "-f", "concat", "-safe", "0", "-i", str(workdir / "frames.txt"),
            "-vf", "split[a][b];[a]palettegen=max_colors=256:stats_mode=full[p];"
                   "[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle",
            "-fps_mode", "passthrough", "-loop", "0", str(path),
        ], check=True)
        print(f"  saved {path.name}: {len(frames)} frames, {path.stat().st_size / 1_048_576:.1f} MB")
    finally:
        shutil.rmtree(workdir, ignore_errors=True)


# ── Scenes ──────────────────────────────────────────────────────────────────


def row(page: Page, subject: str) -> Locator:
    return page.locator("table tbody tr", has_text=subject).first


async def scene_realtime_sync(browser: Browser, org: DemoOrg) -> None:
    print("demo-realtime-sync.gif")
    sarah = await Actor.open(browser, org.admin, GIF_SCALE)
    marcus = await Actor.open(browser, org.agent, GIF_SCALE)
    await asyncio.gather(sarah.goto_live("/tickets"), marcus.goto_live("/tickets"))

    subject = "Checkout page returns 500 error"
    page = sarah.page
    panes = [
        Pane(sarah, "Sarah Chen  ·  Tenant admin", "Browser 1  ·  Acme Inc.", ADMIN),
        Pane(marcus, "Marcus Lee  ·  Support agent", "Browser 2  ·  Acme Inc.", AGENT),
    ]
    async with Recorder(panes) as rec:
        rec.caption = "Two people in the same organization, each in their own browser"
        await rec.hold(1.3)
        rec.caption = "Sarah opens a ticket…"
        await sarah.select(page.locator("form.inline-form label:has-text('Queue') select"), "Billing")
        await sarah.type(page.get_by_label("Subject"), subject)
        await sarah.select(page.locator("form.inline-form label:has-text('Priority') select"), "High")
        await sarah.click(page.get_by_role("button", name="Open ticket"))
        rec.caption = "…and it lands in Marcus's list instantly — no refresh, no polling"
        await row(marcus.page, subject).wait_for(timeout=10_000)
        await rec.hold(2.6)

        rec.caption = "Sarah assigns it to Marcus — his row updates in place"
        await sarah.click(row(page, subject), pause=0.6)
        await sarah.click(page.get_by_role("button", name="Assign", exact=True), pause=0.5)
        modal_select = page.locator(".modal select")
        await sarah.select(modal_select, "Marcus Lee (@marcus)")
        await sarah.click(page.locator(".modal button.btn-primary"))
        await row(marcus.page, subject).get_by_text("assigned").wait_for(timeout=10_000)
        await rec.hold(2.4)
    rec.save_gif("demo-realtime-sync.gif")
    await sarah.page.context.close()
    await marcus.page.context.close()


async def scene_ticket_workflow(browser: Browser, org: DemoOrg) -> None:
    print("demo-ticket-workflow.gif")
    marcus = await Actor.open(browser, org.agent, GIF_SCALE, viewport=(1000, 640))
    await marcus.goto_live("/tickets")
    page = marcus.page
    subject = "VPN disconnects every 30 minutes"

    panes = [Pane(marcus, "Marcus Lee  ·  Support agent", "Ticket lifecycle enforced by the domain model", AGENT)]
    async with Recorder(panes) as rec:
        rec.caption = "Assigned  →  In progress"
        await marcus.click(row(page, subject), pause=0.4)
        await page.locator(".ticket-detail").scroll_into_view_if_needed()
        await rec.hold(0.8)
        await marcus.click(page.get_by_role("button", name="Start progress"), pause=1.0)
        rec.caption = "Comments live inside the Ticket aggregate"
        await marcus.type(page.get_by_placeholder("Write a comment"),
                          "Root cause: idle timeout on the VPN gateway. Raised it to 8h.")
        await marcus.click(page.get_by_role("button", name="Post"), pause=1.2)
        rec.caption = "In progress  →  Resolved  →  Closed"
        await marcus.click(page.get_by_role("button", name="Resolve"), pause=1.2)
        await marcus.click(page.get_by_role("button", name="Close", exact=True), pause=1.6)
        rec.caption = "Closed tickets reject further changes — invariants live in the domain"
        await rec.hold(2.0)
    rec.save_gif("demo-ticket-workflow.gif")
    await page.context.close()


async def scene_tenant_isolation(browser: Browser, org: DemoOrg) -> None:
    print("demo-tenant-isolation.gif")
    sarah = await Actor.open(browser, org.admin, GIF_SCALE)
    lena = await Actor.open(browser, None, GIF_SCALE)
    await sarah.goto_live("/tickets")
    await lena.page.goto(f"{BASE_URL}/register")

    panes = [
        Pane(sarah, "Sarah Chen  ·  Acme Inc.", "Tenant database: LiveSync_Tenant_2", ADMIN),
        Pane(lena, "Lena Novak  ·  Globex Corp", "Registering a new organization", AGENT),
    ]
    page = lena.page
    suffix = str(int(time.time()))[-4:]
    async with Recorder(panes) as rec:
        rec.caption = "Registering creates a new tenant with its own SQL Server database"
        await rec.hold(0.8)
        await lena.type(page.get_by_label("Organization / tenant name"), "Globex Corp")
        await lena.type(page.get_by_label("Display name"), "Lena Novak")
        await lena.type(page.get_by_label("Username"), f"lena{suffix}")
        await page.get_by_label("Email").fill(f"lena{suffix}@globex.test")
        await page.get_by_label("Password").fill(PASSWORD)
        await lena.click(page.get_by_role("button", name="Create account"))
        await page.wait_for_url("**/tickets", timeout=30_000)
        await page.get_by_text("signalr · live").first.wait_for(timeout=30_000)
        tenant_id = await page.evaluate("JSON.parse(localStorage.getItem('livesync.auth')).tenantId")
        panes[1].subtitle = f"Tenant database: LiveSync_Tenant_{tenant_id}"
        await rec.hold(1.2)

        rec.caption = "Acme's new ticket never reaches Globex — separate database, separate SignalR group"
        subject = "Renew SSL certificate for api.acme.test"
        await sarah.type(sarah.page.get_by_label("Subject"), subject)
        await sarah.click(sarah.page.get_by_role("button", name="Open ticket"))
        await row(sarah.page, subject).wait_for(timeout=10_000)
        await rec.hold(2.6)
    rec.save_gif("demo-tenant-isolation.gif")
    await sarah.page.context.close()
    await page.context.close()


async def static_screenshots(browser: Browser, org: DemoOrg) -> None:
    print("Static screenshots")
    sarah = await Actor.open(browser, org.admin, SHOT_SCALE, viewport=(1280, 800), cursor=False)
    page = sarah.page

    await sarah.goto_live("/tickets")
    await row(page, "SSO login fails for contractors").click()
    await page.locator(".comment-item").first.wait_for(timeout=10_000)
    await page.wait_for_timeout(500)
    await page.screenshot(path=str(ASSETS / "screenshot-tickets.png"), full_page=True)

    await sarah.goto_live("/queues")
    await page.screenshot(path=str(ASSETS / "screenshot-queues.png"))

    for link, heading, name in [
        ("Admin", "Overview", "screenshot-admin-overview.png"),
        ("Audit", "Audit log", "screenshot-admin-audit.png"),
    ]:
        await page.get_by_role("link", name=link, exact=True).click()
        await page.get_by_role("heading", name=heading).first.wait_for(timeout=15_000)
        await page.wait_for_timeout(1200)
        await page.screenshot(path=str(ASSETS / name))
    print("  saved 4 screenshots")
    await page.context.close()


async def main() -> int:
    try:
        requests.get(f"{BASE_URL}/health", timeout=5).raise_for_status()
    except requests.RequestException:
        raise SystemExit("LiveSync API is not reachable at http://localhost:5252 — start Docker, API and Worker first.")

    ASSETS.mkdir(parents=True, exist_ok=True)
    org = seed_demo_org()
    only = set(sys.argv[1:])

    async with async_playwright() as playwright:
        browser = await playwright.chromium.launch()
        try:
            scenes = {
                "realtime": scene_realtime_sync,
                "workflow": scene_ticket_workflow,
                "isolation": scene_tenant_isolation,
                "screenshots": static_screenshots,
            }
            for key, scene in scenes.items():
                if not only or key in only:
                    await scene(browser, org)
        finally:
            await browser.close()

    print(f"Done. Assets written to {ASSETS}")
    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
