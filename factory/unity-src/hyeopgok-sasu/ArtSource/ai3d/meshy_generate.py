#!/usr/bin/env python3
"""Explicit, resumable Meshy generation for Hyeopgok Sasu AI assets.

The default invocation is a local dry run. A task is created only when both an
asset name and ``--submit`` are supplied. API credentials and signed download
URLs are deliberately kept out of stdout and the persisted session log.
"""

from __future__ import annotations

import argparse
import base64
import fcntl
import hashlib
import json
import os
import re
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Tuple


API_ROOT = "https://api.meshy.ai/openapi/v1/"
CREDIT_CAP = 400
ESTIMATED_CREDITS = 30
POLL_SECONDS = 15
POLL_TIMEOUT_SECONDS = 30 * 60

AI3D_DIR = Path(__file__).resolve().parent
CONCEPT_DIR = AI3D_DIR / "concepts"
SESSION_PATH = AI3D_DIR / "meshy-session.json"
KEY_PATH = Path.home() / ".config/mosslight/meshy-api-key.txt"


def find_repo_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / ".git").exists():
            return candidate
    raise RuntimeError("Repository root not found")


REPO_ROOT = find_repo_root(AI3D_DIR)
RAW_ROOT = REPO_ROOT / "scratchpad" / "ai3d-raw" / "hyeopgok-sasu-a"
LOCK_PATH = RAW_ROOT / ".meshy-session.lock"

CONCEPTS: Dict[str, Path] = {
    "king": CONCEPT_DIR / "king-v4.png",
    "ally-soldier": CONCEPT_DIR / "ally-soldier-v2.png",
    "enemy-soldier": CONCEPT_DIR / "enemy-soldier-v2.png",
    "crossbow-tower": CONCEPT_DIR / "crossbow-tower-v2.png",
    "barracks": CONCEPT_DIR / "barracks-v2.png",
    "enemy-giant": CONCEPT_DIR / "enemy-giant-v2.png",
}
# Pose normalization can discard held props. Keep it only for the unarmed king;
# armed units retain the source pose so sword, shield, spear, and cleaver survive.
HUMANOIDS = {"king"}

COMMON_PARAMETERS: Dict[str, Any] = {
    "ai_model": "meshy-7.1",
    "model_type": "standard",
    "geometry_resolution": "standard",
    "should_texture": True,
    "enable_pbr": True,
    "texture_resolution": "4k",
    "should_remesh": False,
    "image_enhancement": False,
    "target_formats": ["glb"],
    "auto_size": True,
    "origin_at": "bottom",
    "multi_view_thumbnails": True,
}

TERMINAL_STATUSES = {"SUCCEEDED", "FAILED", "CANCELED", "EXPIRED"}
FAILED_STATUSES = {"FAILED", "CANCELED", "EXPIRED"}
SAFE_VIEWS = ("front", "right", "back", "left")
SAFE_TEXTURE_KINDS = ("base_color", "metallic", "normal", "roughness", "emission")


class SafeError(RuntimeError):
    """An error whose message is safe to display and persist."""


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def repo_relative(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(path.resolve())


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parameters_for(asset: str) -> Dict[str, Any]:
    parameters = dict(COMMON_PARAMETERS)
    parameters["target_formats"] = list(COMMON_PARAMETERS["target_formats"])
    if asset in HUMANOIDS:
        parameters["pose_mode"] = "a-pose"
    return parameters


def job_key(asset: str, variant: int) -> str:
    return f"{asset}-v{variant}"


def output_dir(asset: str, variant: int) -> Path:
    return RAW_ROOT / job_key(asset, variant)


def read_key() -> str:
    key = os.environ.get("MESHY_API_KEY", "").strip()
    if not key:
        try:
            key = KEY_PATH.read_text(encoding="utf-8").strip()
        except OSError as exc:
            raise SafeError(f"Meshy key unavailable ({type(exc).__name__})") from None
    if not key:
        raise SafeError("Meshy key is empty")
    return key


def api_json(
    key: str,
    path: str,
    data: Optional[Dict[str, Any]] = None,
    timeout: int = 90,
) -> Dict[str, Any]:
    body = json.dumps(data, separators=(",", ":")).encode("utf-8") if data is not None else None
    request = urllib.request.Request(
        API_ROOT + path,
        data=body,
        headers={"Authorization": "Bearer " + key, "Content-Type": "application/json"},
        method="POST" if data is not None else "GET",
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            result = json.load(response)
    except urllib.error.HTTPError as exc:
        # Response bodies may echo request material, so never expose or persist them.
        raise SafeError(f"Meshy API HTTP {exc.code}") from None
    except urllib.error.URLError as exc:
        reason_name = type(exc.reason).__name__
        raise SafeError(f"Meshy API network error ({reason_name})") from None
    except (TimeoutError, json.JSONDecodeError) as exc:
        raise SafeError(f"Meshy API response error ({type(exc).__name__})") from None
    if not isinstance(result, dict):
        raise SafeError("Meshy API returned an unexpected response type")
    return result


def get_balance(key: str) -> int:
    result = api_json(key, "balance", timeout=30)
    balance = result.get("balance")
    if isinstance(balance, bool) or not isinstance(balance, (int, float)):
        raise SafeError("Meshy balance response was not numeric")
    if balance < 0 or int(balance) != balance:
        raise SafeError("Meshy balance response was not a non-negative integer")
    return int(balance)


def load_session() -> Optional[Dict[str, Any]]:
    if not SESSION_PATH.exists():
        return None
    try:
        session = json.loads(SESSION_PATH.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SafeError(f"Session log is unreadable ({type(exc).__name__})") from None
    if not isinstance(session, dict) or not isinstance(session.get("jobs"), dict):
        raise SafeError("Session log has an unexpected schema")
    policy = session.get("creditPolicy") or {}
    if policy.get("cap") != CREDIT_CAP or policy.get("estimatePerJob") != ESTIMATED_CREDITS:
        raise SafeError("Session credit policy does not match this script")
    if not isinstance(session.get("balanceStart"), int):
        raise SafeError("Session log has no valid balanceStart")
    return session


def atomic_save(session: Dict[str, Any]) -> None:
    SESSION_PATH.parent.mkdir(parents=True, exist_ok=True)
    refresh_summary(session)
    temporary = SESSION_PATH.with_name(SESSION_PATH.name + ".tmp")
    temporary.write_text(
        json.dumps(session, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    temporary.replace(SESSION_PATH)


def active_reservation(job: Dict[str, Any]) -> int:
    if not job.get("submissionStarted"):
        return 0
    if job.get("status") in TERMINAL_STATUSES or job.get("downloadStatus") == "DOWNLOADED":
        return 0
    consumed = job.get("consumedCredits")
    known = consumed if isinstance(consumed, int) and consumed >= 0 else 0
    estimate = job.get("estimatedCredits", ESTIMATED_CREDITS)
    if not isinstance(estimate, int) or estimate < 0:
        estimate = ESTIMATED_CREDITS
    return max(0, estimate - known)


def credit_snapshot(session: Dict[str, Any], current_balance: Optional[int] = None) -> Dict[str, int]:
    jobs = session.get("jobs", {})
    known = 0
    reserved = 0
    for job in jobs.values():
        if not isinstance(job, dict):
            continue
        consumed = job.get("consumedCredits")
        if isinstance(consumed, int) and consumed >= 0:
            known += consumed
        reserved += active_reservation(job)

    if current_balance is None:
        last = session.get("balanceLast")
        current_balance = last if isinstance(last, int) else session["balanceStart"]
    balance_delta = max(0, session["balanceStart"] - current_balance)
    accounted = max(balance_delta, known + reserved)
    return {
        "sumKnownConsumed": known,
        "activeReservation": reserved,
        "balanceDelta": balance_delta,
        "accountedSpend": accounted,
        "remainingCap": max(0, CREDIT_CAP - accounted),
    }


def refresh_summary(session: Dict[str, Any]) -> None:
    session["summary"] = credit_snapshot(session)
    session["updatedAt"] = now_iso()


def record_balance(session: Dict[str, Any], balance: int) -> None:
    session["balanceLast"] = balance


def new_session(balance: int) -> Dict[str, Any]:
    timestamp = now_iso()
    return {
        "schemaVersion": 1,
        "provider": "Meshy",
        "startedAt": timestamp,
        "updatedAt": timestamp,
        "balanceStart": balance,
        "balanceLast": balance,
        "balanceFloor": balance - CREDIT_CAP,
        "creditPolicy": {
            "cap": CREDIT_CAP,
            "estimatePerJob": ESTIMATED_CREDITS,
            "rule": "max(balanceDelta,sumKnownConsumed+activeReservation)+nextEstimate<=cap",
        },
        "jobs": {},
        "summary": {},
    }


def validate_job_identity(
    job: Dict[str, Any], source_sha256: str, parameters: Dict[str, Any]
) -> None:
    if job.get("sourceSha256") != source_sha256:
        raise SafeError("Concept changed for this variant; submit with a new --variant")
    if job.get("parameters") != parameters:
        raise SafeError("Parameters changed for this variant; submit with a new --variant")


def make_job(asset: str, variant: int, source_sha256: str, parameters: Dict[str, Any]) -> Dict[str, Any]:
    destination = output_dir(asset, variant)
    return {
        "asset": asset,
        "variant": variant,
        "conceptPath": repo_relative(CONCEPTS[asset]),
        "sourceSha256": source_sha256,
        "outputDir": repo_relative(destination),
        "parameters": parameters,
        "estimatedCredits": ESTIMATED_CREDITS,
        "consumedCredits": None,
        "submissionStarted": False,
        "status": "NOT_SUBMITTED",
        "files": [],
        "createdAt": now_iso(),
    }


def safe_filename_piece(value: str) -> str:
    return re.sub(r"[^a-z0-9_-]+", "-", value.lower()).strip("-")


def download_file(url: str, destination: Path) -> Dict[str, Any]:
    if not isinstance(url, str) or not url.startswith("https://"):
        raise SafeError(f"Missing or invalid download URL for {destination.name}")
    destination.parent.mkdir(parents=True, exist_ok=True)
    partial = destination.with_name(destination.name + ".part")
    if not destination.exists():
        try:
            with urllib.request.urlopen(url, timeout=180) as response:
                with partial.open("wb") as output:
                    while True:
                        chunk = response.read(1024 * 1024)
                        if not chunk:
                            break
                        output.write(chunk)
            partial.replace(destination)
        except urllib.error.HTTPError as exc:
            raise SafeError(f"Download HTTP {exc.code} for {destination.name}") from None
        except urllib.error.URLError as exc:
            reason_name = type(exc.reason).__name__
            raise SafeError(f"Download network error ({reason_name}) for {destination.name}") from None
        except TimeoutError:
            raise SafeError(f"Download timeout for {destination.name}") from None
    if not destination.is_file() or destination.stat().st_size <= 0:
        raise SafeError(f"Downloaded file is empty: {destination.name}")
    return {
        "path": repo_relative(destination),
        "bytes": destination.stat().st_size,
        "sha256": sha256_file(destination),
    }


def download_specs(result: Dict[str, Any]) -> List[Tuple[str, str]]:
    specs: List[Tuple[str, str]] = []
    model_urls = result.get("model_urls")
    glb_url = model_urls.get("glb") if isinstance(model_urls, dict) else None
    if not isinstance(glb_url, str):
        raise SafeError("Succeeded task did not provide a GLB")
    specs.append(("model.glb", glb_url))

    thumbnail = result.get("thumbnail_url")
    if isinstance(thumbnail, str) and thumbnail:
        specs.append(("preview.png", thumbnail))

    thumbnails = result.get("thumbnail_urls")
    if isinstance(thumbnails, dict):
        for view in SAFE_VIEWS:
            url = thumbnails.get(view)
            if isinstance(url, str) and url:
                specs.append((f"preview-{view}.png", url))

    texture_sets = result.get("texture_urls")
    if isinstance(texture_sets, list):
        for index, material in enumerate(texture_sets):
            if not isinstance(material, dict):
                continue
            for kind in SAFE_TEXTURE_KINDS:
                url = material.get(kind)
                if isinstance(url, str) and url:
                    safe_kind = safe_filename_piece(kind)
                    specs.append((f"texture-{index}-{safe_kind}.png", url))
    return specs


def update_from_task(job: Dict[str, Any], result: Dict[str, Any]) -> None:
    status = result.get("status")
    if not isinstance(status, str):
        raise SafeError("Task response had no valid status")
    job["status"] = status
    progress = result.get("progress")
    if isinstance(progress, (int, float)) and not isinstance(progress, bool):
        job["progress"] = progress
    consumed = result.get("consumed_credits")
    if isinstance(consumed, int) and consumed >= 0:
        job["consumedCredits"] = consumed
    job["lastPolledAt"] = now_iso()


def finish_balance(key: str, session: Dict[str, Any], job: Dict[str, Any]) -> None:
    balance = get_balance(key)
    job["balanceAfterFinal"] = balance
    record_balance(session, balance)
    atomic_save(session)


def poll_and_download(
    key: str,
    session: Dict[str, Any],
    job: Dict[str, Any],
    destination: Path,
) -> None:
    task_id = job.get("taskId")
    if not isinstance(task_id, str) or not task_id:
        raise SafeError("Cannot resume: taskId is missing")

    if "balanceAfterPost" not in job:
        balance_after_post = get_balance(key)
        job["balanceAfterPost"] = balance_after_post
        record_balance(session, balance_after_post)
        atomic_save(session)

    deadline = time.monotonic() + POLL_TIMEOUT_SECONDS
    previous: Optional[Tuple[Any, Any]] = None
    final_result: Optional[Dict[str, Any]] = None
    while time.monotonic() < deadline:
        result = api_json(key, "image-to-3d/" + task_id)
        update_from_task(job, result)
        atomic_save(session)
        progress = (job.get("status"), job.get("progress"))
        if progress != previous:
            print(f"{job['asset']} {progress[0]} {progress[1]}", flush=True)
            previous = progress
        if job["status"] in TERMINAL_STATUSES:
            final_result = result
            break
        time.sleep(POLL_SECONDS)

    if final_result is None:
        raise SafeError("Task remains active; rerun the same command with --submit to resume")

    finish_balance(key, session, job)
    if job["status"] in FAILED_STATUSES:
        job["finishedAt"] = now_iso()
        atomic_save(session)
        raise SafeError(f"Meshy task ended with status {job['status']}; use a new --variant")

    files: List[Dict[str, Any]] = []
    for filename, url in download_specs(final_result):
        files.append(download_file(url, destination / filename))
    job["files"] = files
    job["downloadStatus"] = "DOWNLOADED"
    job["finishedAt"] = now_iso()
    atomic_save(session)
    print(f"Downloaded {job['asset']} to {repo_relative(destination)}", flush=True)


def enforce_budget(session: Dict[str, Any], balance_before: int) -> Dict[str, int]:
    snapshot = credit_snapshot(session, current_balance=balance_before)
    projected = snapshot["accountedSpend"] + ESTIMATED_CREDITS
    floor = session["balanceFloor"]
    if projected > CREDIT_CAP:
        raise SafeError(
            f"Credit cap would be exceeded ({snapshot['accountedSpend']}+"
            f"{ESTIMATED_CREDITS}>{CREDIT_CAP})"
        )
    if balance_before - ESTIMATED_CREDITS < floor:
        raise SafeError("Session balance floor would be crossed")
    if balance_before < ESTIMATED_CREDITS:
        raise SafeError("Insufficient Meshy balance for the estimated task cost")
    return snapshot


def submit_or_resume(asset: str, variant: int) -> None:
    source = CONCEPTS[asset]
    if not source.is_file():
        raise SafeError(f"Concept is missing: {repo_relative(source)}")
    source_bytes = source.read_bytes()
    source_sha256 = sha256_bytes(source_bytes)
    parameters = parameters_for(asset)
    destination = output_dir(asset, variant)

    key = read_key()
    AI3D_DIR.mkdir(parents=True, exist_ok=True)
    RAW_ROOT.mkdir(parents=True, exist_ok=True)
    with LOCK_PATH.open("a+", encoding="utf-8") as lock:
        fcntl.flock(lock.fileno(), fcntl.LOCK_EX)
        session = load_session()
        if session is None:
            session = new_session(get_balance(key))
            atomic_save(session)

        key_name = job_key(asset, variant)
        job = session["jobs"].get(key_name)
        if job is None:
            job = make_job(asset, variant, source_sha256, parameters)
            session["jobs"][key_name] = job
            atomic_save(session)
        elif not isinstance(job, dict):
            raise SafeError("Session job entry has an unexpected schema")
        else:
            validate_job_identity(job, source_sha256, parameters)

        if job.get("downloadStatus") == "DOWNLOADED":
            print(f"Already downloaded: {repo_relative(destination)}")
            return
        if job.get("status") in FAILED_STATUSES:
            raise SafeError(f"Prior task is {job['status']}; use a new --variant")
        if job.get("submissionStarted") and not job.get("taskId"):
            raise SafeError(
                "Prior submission is uncertain; reconcile the Meshy dashboard before any retry"
            )

        if not job.get("taskId"):
            balance_before = get_balance(key)
            record_balance(session, balance_before)
            snapshot = enforce_budget(session, balance_before)
            job.update(
                balanceBefore=balance_before,
                budgetAccountedBefore=snapshot["accountedSpend"],
                submissionStarted=True,
                submissionStartedAt=now_iso(),
                status="SUBMITTING",
            )
            atomic_save(session)

            payload = dict(parameters)
            payload["image_url"] = "data:image/png;base64," + base64.b64encode(source_bytes).decode("ascii")
            response = api_json(key, "image-to-3d", payload)
            task_id = response.get("result")
            if not isinstance(task_id, str) or not task_id:
                # submissionStarted deliberately remains true: a retry could double-charge.
                atomic_save(session)
                raise SafeError(
                    "Submission response had no taskId; reconcile the Meshy dashboard before retrying"
                )
            job["taskId"] = task_id
            job["submittedAt"] = now_iso()
            job["status"] = "PENDING"
            atomic_save(session)

            balance_after_post = get_balance(key)
            job["balanceAfterPost"] = balance_after_post
            record_balance(session, balance_after_post)
            atomic_save(session)
            print(f"Submitted {asset} as {key_name}", flush=True)

        poll_and_download(key, session, job, destination)


def dry_run(assets: Iterable[str], variant: int) -> None:
    entries: List[Dict[str, Any]] = []
    missing: List[str] = []
    for asset in assets:
        source = CONCEPTS[asset]
        if not source.is_file():
            missing.append(repo_relative(source))
            continue
        entries.append(
            {
                "asset": asset,
                "variant": variant,
                "conceptPath": repo_relative(source),
                "sourceSha256": sha256_file(source),
                "outputDir": repo_relative(output_dir(asset, variant)),
                "parameters": parameters_for(asset),
                "estimatedCredits": ESTIMATED_CREDITS,
                "submitted": False,
            }
        )
    report = {
        "dryRun": True,
        "creditCap": CREDIT_CAP,
        "estimatedCreditsPerSubmission": ESTIMATED_CREDITS,
        "sessionPath": repo_relative(SESSION_PATH),
        "rawRoot": repo_relative(RAW_ROOT),
        "assets": entries,
        "missingConcepts": missing,
    }
    print(json.dumps(report, ensure_ascii=False, indent=2, sort_keys=True))
    if missing:
        raise SafeError("One or more fixed concept images are missing")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("asset", nargs="?", choices=tuple(CONCEPTS))
    parser.add_argument("--variant", type=int, default=1)
    parser.add_argument(
        "--submit",
        action="store_true",
        help="explicitly permit one Meshy POST, or resume that asset/variant",
    )
    args = parser.parse_args()
    if args.variant < 1:
        parser.error("--variant must be at least 1")
    if args.submit and not args.asset:
        parser.error("an asset is required with --submit; batch submission is intentionally disabled")
    return args


def main() -> int:
    args = parse_args()
    try:
        if not args.submit:
            dry_run((args.asset,) if args.asset else CONCEPTS.keys(), args.variant)
            return 0
        submit_or_resume(args.asset, args.variant)
        return 0
    except SafeError as exc:
        print(f"ERROR: {exc}", flush=True)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
