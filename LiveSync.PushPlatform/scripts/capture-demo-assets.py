#!/usr/bin/env python3
"""Capture README demo GIFs and screenshots from a running LiveSync dev stack."""

from __future__ import annotations

import json
import sys
import time
from io import BytesIO
from pathlib import Path

import requests
from PIL import Image
from playwright.sync_api import Browser, Page, sync_playwright

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "docs" / "assets"
BASE_URL = "http://localhost:5252"
VIEWPORT = {"width": 1280, "height": 720}


def ensure_api() -> None:
    try:
        response = requests.get(f"{BASE_URL}/", timeout=5)
        response.raise_for_status()
    except requests.RequestException as exc:
        raise SystemExit(
            "LiveSync API is not reachable at http://localhost:5252. "
            "Start Docker + API + Worker first (see scripts/dev.ps1)."
        ) from exc


def dev_token(tenant_id: int, user_id: int, user_name: str) -> dict:
    response = requests.post(
        f"{BASE_URL}/dev/auth/token",
        json={"tenantId": tenant_id, "userId": user_id, "userName": user_name},
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


def auth_headers(token: str) -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


def inject_session(page: Page, tenant_id: int, user_id: int, user_name: str, token_payload: dict) -> None:
    session = {
        "accessToken": token_payload["accessToken"],
        "expiresAtUtc": token_payload["expiresAtUtc"],
        "tenantId": tenant_id,
        "userId": user_id,
        "userName": user_name,
    }
    page.goto(f"{BASE_URL}/login")
    page.evaluate(
        "(session) => localStorage.setItem('livesync.auth', JSON.stringify(session))",
        session,
    )


def wait_for_live(page: Page) -> None:
    page.wait_for_selector("text=signalr", timeout=30_000)
    page.wait_for_timeout(800)


def screenshot(page: Page, name: str) -> Path:
    path = ASSETS / name
    page.screenshot(path=str(path), full_page=False)
    print(f"  saved {path.name}")
    return path


def capture_frame(page: Page) -> Image.Image:
    data = page.screenshot(type="png")
    return Image.open(BytesIO(data)).convert("RGB")


def save_gif(frames: list[Image.Image], name: str, duration_ms: int = 900) -> Path:
    path = ASSETS / name
    resized = [frame.resize((960, 540), Image.Resampling.LANCZOS) for frame in frames]
    resized[0].save(
        path,
        save_all=True,
        append_images=resized[1:],
        duration=duration_ms,
        loop=0,
        optimize=True,
    )
    print(f"  saved {path.name} ({len(frames)} frames)")
    return path


def composite_side_by_side(left: Image.Image, right: Image.Image) -> Image.Image:
    left = left.resize((640, 360), Image.Resampling.LANCZOS)
    right = right.resize((640, 360), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (1280, 360), (15, 17, 21))
    canvas.paste(left, (0, 0))
    canvas.paste(right, (640, 0))
    return canvas


def ensure_member_user(admin_token: str) -> tuple[int, str]:
    headers = auth_headers(admin_token)
    users_response = requests.get(f"{BASE_URL}/api/v1/auth/users", headers=headers, timeout=30)
    if users_response.ok:
        for user in users_response.json():
            if user["userName"] == "member1":
                return user["userId"], "member1"

    created = requests.post(
        f"{BASE_URL}/api/v1/auth/dev/users",
        json={
            "tenantId": 1,
            "userName": "member1",
            "email": "member1@livesync.local",
            "password": "Member123!",
            "displayName": "Member One",
            "assignAdminRole": False,
        },
        timeout=30,
    )
    created.raise_for_status()
    payload = created.json()
    return payload["userId"], payload["userName"]


def open_ticket(token: str, subject: str, queue_id: int = 1, reporter_id: int = 1) -> int:
    response = requests.post(
        f"{BASE_URL}/api/v1/tickets",
        headers=auth_headers(token),
        json={
            "queueId": queue_id,
            "subject": subject,
            "description": subject,
            "priority": 1,
            "reporterUserId": reporter_id,
        },
        timeout=30,
    )
    response.raise_for_status()
    return int(response.json())


def wait_for_admin_ready(page: Page) -> None:
    page.goto(f"{BASE_URL}/tickets")
    wait_for_live(page)
    page.get_by_role("link", name="Admin").wait_for(timeout=30_000)
    page.get_by_role("link", name="Admin").click()
    page.get_by_role("heading", name="Overview").wait_for(timeout=15_000)
    page.wait_for_timeout(1200)


def capture_static_screenshots(browser: Browser, admin_token_payload: dict) -> None:
    print("Static screenshots")
    context = browser.new_context(viewport=VIEWPORT)
    page = context.new_page()
    inject_session(page, 1, 1, "admin@livesync.local", admin_token_payload)

    page.goto(f"{BASE_URL}/tickets")
    wait_for_live(page)
    rows = page.locator("table tbody tr")
    if rows.count() > 0:
        rows.first.click()
        page.wait_for_timeout(500)
    screenshot(page, "screenshot-tickets.png")

    page.goto(f"{BASE_URL}/queues")
    wait_for_live(page)
    screenshot(page, "screenshot-queues.png")

    wait_for_admin_ready(page)
    screenshot(page, "demo-admin-console.png")

    context.close()


def capture_realtime_gif(browser: Browser, admin_token_payload: dict, member_id: int) -> None:
    print("demo-realtime-sync.gif")
    admin_ctx = browser.new_context(viewport={"width": 640, "height": 360})
    member_ctx = browser.new_context(viewport={"width": 640, "height": 360})
    admin_page = admin_ctx.new_page()
    member_page = member_ctx.new_page()

    member_token_payload = dev_token(1, member_id, "member1")
    inject_session(admin_page, 1, 1, "admin@livesync.local", admin_token_payload)
    inject_session(member_page, 1, member_id, "member1", member_token_payload)

    admin_page.goto(f"{BASE_URL}/tickets")
    member_page.goto(f"{BASE_URL}/tickets")
    wait_for_live(admin_page)
    wait_for_live(member_page)

    frames: list[Image.Image] = []
    frames.append(composite_side_by_side(capture_frame(admin_page), capture_frame(member_page)))

    admin_access_token = admin_token_payload["accessToken"]
    member_access_token = member_token_payload["accessToken"]
    ticket_id = open_ticket(admin_access_token, "Live sync demo ticket")
    for _ in range(8):
        member_page.wait_for_timeout(350)
        frames.append(composite_side_by_side(capture_frame(admin_page), capture_frame(member_page)))

    member_page.locator(f"text=Live sync demo ticket").first.click()
    member_page.wait_for_timeout(400)
    frames.append(composite_side_by_side(capture_frame(admin_page), capture_frame(member_page)))

    requests.post(
        f"{BASE_URL}/api/v1/tickets/{ticket_id}/comments",
        headers=auth_headers(member_access_token),
        json={"authorUserId": member_id, "body": "Synced comment from member tab"},
        timeout=30,
    ).raise_for_status()

    for _ in range(6):
        admin_page.wait_for_timeout(350)
        admin_page.locator(f"text=Live sync demo ticket").first.click()
        frames.append(composite_side_by_side(capture_frame(admin_page), capture_frame(member_page)))

    save_gif(frames, "demo-realtime-sync.gif", duration_ms=700)
    admin_ctx.close()
    member_ctx.close()


def capture_workflow_gif(browser: Browser, admin_token_payload: dict) -> None:
    print("demo-ticket-workflow.gif")
    context = browser.new_context(viewport=VIEWPORT)
    page = context.new_page()
    inject_session(page, 1, 1, "admin@livesync.local", admin_token_payload)
    page.goto(f"{BASE_URL}/tickets")
    wait_for_live(page)

    admin_access_token = admin_token_payload["accessToken"]
    ticket_id = open_ticket(admin_access_token, "Workflow demo ticket")
    page.reload()
    wait_for_live(page)
    page.locator("text=Workflow demo ticket").first.click()
    page.wait_for_timeout(500)

    frames = [capture_frame(page)]

    page.get_by_role("button", name="Assign").click()
    assignee_select = page.locator(".modal select")
    assignee_select.wait_for(timeout=10_000)
    assignee_select.select_option(index=0)
    page.locator(".modal button.btn-primary").click()
    page.wait_for_timeout(700)
    frames.append(capture_frame(page))

    page.get_by_role("button", name="Start progress").click()
    page.wait_for_timeout(700)
    frames.append(capture_frame(page))

    page.get_by_role("button", name="Resolve").click()
    page.wait_for_timeout(700)
    frames.append(capture_frame(page))

    page.get_by_role("button", name="Close").click()
    page.wait_for_timeout(700)
    frames.append(capture_frame(page))

    save_gif(frames, "demo-ticket-workflow.gif", duration_ms=1100)
    context.close()


def capture_tenant_isolation_gif(browser: Browser, admin_token_payload: dict) -> None:
    print("demo-tenant-isolation.gif")
    tenant_a = browser.new_context(viewport={"width": 640, "height": 360})
    tenant_b = browser.new_context(viewport={"width": 640, "height": 360})
    page_a = tenant_a.new_page()
    page_b = tenant_b.new_page()

    inject_session(page_a, 1, 1, "admin@livesync.local", admin_token_payload)

    page_b.goto(f"{BASE_URL}/register")
    suffix = str(int(time.time()))[-6:]
    page_b.get_by_label("Organization / tenant name").fill(f"Demo Org {suffix}")
    page_b.get_by_label("Display name").fill("Demo Owner")
    page_b.get_by_label("Username").fill(f"owner{suffix}")
    page_b.get_by_label("Email").fill(f"owner{suffix}@demo.local")
    page_b.get_by_label("Password").fill("DemoOwner123!")
    page_b.get_by_role("button", name="Create account").click()
    page_b.wait_for_url("**/tickets", timeout=30_000)
    wait_for_live(page_b)

    page_a.goto(f"{BASE_URL}/tickets")
    wait_for_live(page_a)
    open_ticket(admin_token_payload["accessToken"], f"Tenant 1 only {suffix}")

    frames: list[Image.Image] = []
    for _ in range(5):
        page_a.wait_for_timeout(300)
        frames.append(composite_side_by_side(capture_frame(page_a), capture_frame(page_b)))

    save_gif(frames, "demo-tenant-isolation.gif", duration_ms=900)
    tenant_a.close()
    tenant_b.close()


def main() -> int:
    ASSETS.mkdir(parents=True, exist_ok=True)
    ensure_api()

    admin_token_payload = dev_token(1, 1, "admin@livesync.local")
    admin_token = admin_token_payload["accessToken"]
    member_id, _ = ensure_member_user(admin_token)

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        try:
            capture_static_screenshots(browser, admin_token_payload)
            capture_realtime_gif(browser, admin_token_payload, member_id)
            capture_workflow_gif(browser, admin_token_payload)
            capture_tenant_isolation_gif(browser, admin_token_payload)
        finally:
            browser.close()

    print(f"Done. Assets written to {ASSETS}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
