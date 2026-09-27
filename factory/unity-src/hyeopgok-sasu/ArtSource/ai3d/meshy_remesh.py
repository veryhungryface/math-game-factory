#!/usr/bin/env python3
"""Safely remesh the selected Hyeopgok Sasu Meshy tasks.

The default invocation is read-only and prints a dry-run plan. Exactly one
asset may be submitted at a time with ``asset --submit``. Credentials and
signed download URLs are never written to the shared session or stdout.
"""

from __future__ import annotations

import argparse
import fcntl
import hashlib
import json
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Tuple

import meshy_generate as base


CREDIT_CAP = 400
ESTIMATED_CREDITS = 5
EXPECTED_BALANCE_START = 515
POLL_SECONDS = 10
POLL_TIMEOUT_SECONDS = 30 * 60

SESSION_PATH = base.SESSION_PATH
RAW_ROOT = base.RAW_ROOT
LOCK_PATH = base.LOCK_PATH
REMESH_ROOT = RAW_ROOT / "remeshed"

ASSETS: Dict[str, Dict[str, Any]] = {
    "king": {"sourceJob": "king-v1", "targetPolycount": 5500},
    "ally-soldier": {"sourceJob": "ally-soldier-v2", "targetPolycount": 1300},
    "enemy-soldier": {"sourceJob": "enemy-soldier-v2", "targetPolycount": 1300},
    "crossbow-tower": {"sourceJob": "crossbow-tower-v1", "targetPolycount": 4500},
    "barracks": {"sourceJob": "barracks-v1", "targetPolycount": 4500},
    "enemy-giant": {"sourceJob": "enemy-giant-v1", "targetPolycount": 3500},
}

TERMINAL_STATUSES = {"SUCCEEDED", "FAILED", "CANCELED", "EXPIRED"}
FAILED_STATUSES = {"FAILED", "CANCELED", "EXPIRED"}

SafeError = base.SafeError


def parameters_for(asset: str) -> Dict[str, Any]:
    return {
        "target_formats": ["glb"],
        "topology": "triangle",
        "target_polycount": ASSETS[asset]["targetPolycount"],
    }


def output_path(asset: str) -> Path:
    return REMESH_ROOT / asset / "model.glb"


def source_task_hash(task_id: str) -> str:
    return hashlib.sha256(task_id.encode("utf-8")).hexdigest()


def load_session() -> Dict[str, Any]:
    if not SESSION_PATH.is_file():
        raise SafeError(f"Meshy session is missing: {base.repo_relative(SESSION_PATH)}")
    try:
        session = json.loads(SESSION_PATH.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SafeError(f"Meshy session is unreadable ({type(exc).__name__})") from None
    if not isinstance(session, dict) or not isinstance(session.get("jobs"), dict):
        raise SafeError("Meshy session has an unexpected schema")
    if session.get("balanceStart") != EXPECTED_BALANCE_START:
        raise SafeError(
            f"Expected original balanceStart {EXPECTED_BALANCE_START}, "
            f"found {session.get('balanceStart')!r}"
        )
    if session.get("balanceFloor") != EXPECTED_BALANCE_START - CREDIT_CAP:
        raise SafeError("Meshy session balanceFloor does not match the 400-credit cap")
    policy = session.get("creditPolicy")
    if not isinstance(policy, dict) or policy.get("cap") != CREDIT_CAP:
        raise SafeError("Meshy session credit cap does not match this script")
    remesh_jobs = session.get("remeshJobs")
    if remesh_jobs is not None and not isinstance(remesh_jobs, dict):
        raise SafeError("Meshy session remeshJobs has an unexpected schema")
    return session


def source_job(session: Dict[str, Any], asset: str) -> Tuple[Dict[str, Any], str]:
    source_key = ASSETS[asset]["sourceJob"]
    job = session["jobs"].get(source_key)
    if not isinstance(job, dict):
        raise SafeError(f"Required source job is missing: {source_key}")
    if job.get("status") != "SUCCEEDED":
        raise SafeError(f"Source job is not SUCCEEDED: {source_key}")
    task_id = job.get("taskId")
    if not isinstance(task_id, str) or not task_id:
        raise SafeError(f"Source job has no taskId: {source_key}")
    return job, task_id


def job_reservation(job: Dict[str, Any], default_estimate: int) -> int:
    if not job.get("submissionStarted"):
        return 0
    if job.get("status") in TERMINAL_STATUSES or job.get("downloadStatus") == "DOWNLOADED":
        return 0
    consumed = job.get("consumedCredits")
    known = consumed if isinstance(consumed, int) and consumed >= 0 else 0
    estimate = job.get("estimatedCredits", default_estimate)
    if not isinstance(estimate, int) or estimate < 0:
        estimate = default_estimate
    return max(0, estimate - known)


def credit_snapshot(
    session: Dict[str, Any], current_balance: Optional[int] = None
) -> Dict[str, int]:
    image_known = 0
    remesh_known = 0
    active_reservation = 0

    for job in session.get("jobs", {}).values():
        if not isinstance(job, dict):
            continue
        consumed = job.get("consumedCredits")
        if isinstance(consumed, int) and consumed >= 0:
            image_known += consumed
        active_reservation += job_reservation(job, base.ESTIMATED_CREDITS)

    for job in (session.get("remeshJobs") or {}).values():
        if not isinstance(job, dict):
            continue
        consumed = job.get("consumedCredits")
        if isinstance(consumed, int) and consumed >= 0:
            remesh_known += consumed
        active_reservation += job_reservation(job, ESTIMATED_CREDITS)

    if current_balance is None:
        recorded = session.get("balanceLast")
        current_balance = recorded if isinstance(recorded, int) else EXPECTED_BALANCE_START
    balance_delta = max(0, EXPECTED_BALANCE_START - current_balance)
    known_total = image_known + remesh_known
    accounted = max(balance_delta, known_total + active_reservation)
    return {
        "imageKnownConsumed": image_known,
        "remeshKnownConsumed": remesh_known,
        "sumKnownConsumed": known_total,
        "activeReservation": active_reservation,
        "balanceDelta": balance_delta,
        "accountedSpend": accounted,
        "remainingCap": max(0, CREDIT_CAP - accounted),
    }


def refresh_summary(session: Dict[str, Any]) -> None:
    snapshot = credit_snapshot(session)
    session["summary"] = dict(snapshot)
    session["remeshSummary"] = {
        "estimatedCreditsPerJob": ESTIMATED_CREDITS,
        "jobCount": len(session.get("remeshJobs") or {}),
        **snapshot,
    }
    session["updatedAt"] = base.now_iso()


def atomic_save(session: Dict[str, Any]) -> None:
    refresh_summary(session)
    temporary = SESSION_PATH.with_name(SESSION_PATH.name + ".tmp")
    temporary.write_text(
        json.dumps(session, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    temporary.replace(SESSION_PATH)


def record_balance(session: Dict[str, Any], balance: int) -> None:
    session["balanceLast"] = balance


def enforce_budget(session: Dict[str, Any], balance_before: int) -> Dict[str, int]:
    snapshot = credit_snapshot(session, current_balance=balance_before)
    projected = snapshot["accountedSpend"] + ESTIMATED_CREDITS
    if projected > CREDIT_CAP:
        raise SafeError(
            f"Credit cap would be exceeded ({snapshot['accountedSpend']}+"
            f"{ESTIMATED_CREDITS}>{CREDIT_CAP})"
        )
    if balance_before - ESTIMATED_CREDITS < EXPECTED_BALANCE_START - CREDIT_CAP:
        raise SafeError("Original-session balance floor would be crossed")
    if balance_before < ESTIMATED_CREDITS:
        raise SafeError("Insufficient Meshy balance for the remesh estimate")
    return snapshot


def make_remesh_job(asset: str, source_task_id: str) -> Dict[str, Any]:
    return {
        "asset": asset,
        "sourceJob": ASSETS[asset]["sourceJob"],
        "sourceTaskIdSha256": source_task_hash(source_task_id),
        "parameters": parameters_for(asset),
        "targetPolycount": ASSETS[asset]["targetPolycount"],
        "estimatedCredits": ESTIMATED_CREDITS,
        "consumedCredits": None,
        "outputPath": base.repo_relative(output_path(asset)),
        "submissionStarted": False,
        "status": "NOT_SUBMITTED",
        "files": [],
        "createdAt": base.now_iso(),
    }


def validate_remesh_job(job: Dict[str, Any], asset: str, source_task_id: str) -> None:
    if job.get("asset") != asset:
        raise SafeError("Remesh job asset identity does not match")
    if job.get("sourceJob") != ASSETS[asset]["sourceJob"]:
        raise SafeError("Remesh source job changed")
    if job.get("sourceTaskIdSha256") != source_task_hash(source_task_id):
        raise SafeError("Remesh source task changed")
    if job.get("parameters") != parameters_for(asset):
        raise SafeError("Remesh parameters changed")


def update_from_task(job: Dict[str, Any], result: Dict[str, Any]) -> None:
    status = result.get("status")
    if not isinstance(status, str):
        raise SafeError("Remesh response had no valid status")
    job["status"] = status
    progress = result.get("progress")
    if isinstance(progress, (int, float)) and not isinstance(progress, bool):
        job["progress"] = progress
    consumed = result.get("consumed_credits")
    if isinstance(consumed, int) and consumed >= 0:
        job["consumedCredits"] = consumed
    job["lastPolledAt"] = base.now_iso()


def download_glb(url: str, destination: Path) -> Dict[str, Any]:
    if not isinstance(url, str) or not url.startswith("https://"):
        raise SafeError("Remesh task did not provide a valid GLB download")
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
            raise SafeError(f"Remesh download HTTP {exc.code}") from None
        except urllib.error.URLError as exc:
            raise SafeError(
                f"Remesh download network error ({type(exc.reason).__name__})"
            ) from None
        except TimeoutError:
            raise SafeError("Remesh download timed out") from None
    if not destination.is_file() or destination.stat().st_size <= 0:
        raise SafeError("Remeshed GLB is missing or empty")
    return {
        "path": base.repo_relative(destination),
        "bytes": destination.stat().st_size,
        "sha256": base.sha256_file(destination),
    }


def poll_and_download(
    key: str,
    session: Dict[str, Any],
    job: Dict[str, Any],
    asset: str,
) -> None:
    task_id = job.get("taskId")
    if not isinstance(task_id, str) or not task_id:
        raise SafeError("Cannot resume remesh: taskId is missing")

    if "balanceAfterPost" not in job:
        balance_after_post = base.get_balance(key)
        job["balanceAfterPost"] = balance_after_post
        record_balance(session, balance_after_post)
        atomic_save(session)

    deadline = time.monotonic() + POLL_TIMEOUT_SECONDS
    previous: Optional[Tuple[Any, Any]] = None
    final_result: Optional[Dict[str, Any]] = None
    while time.monotonic() < deadline:
        result = base.api_json(key, "remesh/" + task_id)
        update_from_task(job, result)
        atomic_save(session)
        progress = (job.get("status"), job.get("progress"))
        if progress != previous:
            print(f"{asset} remesh {progress[0]} {progress[1]}", flush=True)
            previous = progress
        if job["status"] in TERMINAL_STATUSES:
            final_result = result
            break
        time.sleep(POLL_SECONDS)

    if final_result is None:
        raise SafeError("Remesh remains active; rerun the same command with --submit")

    balance_after_final = base.get_balance(key)
    job["balanceAfterFinal"] = balance_after_final
    record_balance(session, balance_after_final)
    atomic_save(session)

    if job["status"] in FAILED_STATUSES:
        job["finishedAt"] = base.now_iso()
        atomic_save(session)
        raise SafeError(f"Remesh ended with status {job['status']}")

    model_urls = final_result.get("model_urls")
    glb_url = model_urls.get("glb") if isinstance(model_urls, dict) else None
    file_record = download_glb(glb_url, output_path(asset))
    job["files"] = [file_record]
    job["downloadStatus"] = "DOWNLOADED"
    job["finishedAt"] = base.now_iso()
    atomic_save(session)
    print(f"Downloaded remesh for {asset} to {file_record['path']}", flush=True)


def submit_or_resume(asset: str) -> None:
    key = base.read_key()
    RAW_ROOT.mkdir(parents=True, exist_ok=True)
    with LOCK_PATH.open("a+", encoding="utf-8") as lock:
        fcntl.flock(lock.fileno(), fcntl.LOCK_EX)
        session = load_session()
        _, source_task_id = source_job(session, asset)
        remesh_jobs = session.setdefault("remeshJobs", {})
        job = remesh_jobs.get(asset)
        if job is None:
            job = make_remesh_job(asset, source_task_id)
            remesh_jobs[asset] = job
            atomic_save(session)
        elif not isinstance(job, dict):
            raise SafeError("Remesh job has an unexpected schema")
        else:
            validate_remesh_job(job, asset, source_task_id)

        if job.get("downloadStatus") == "DOWNLOADED":
            print(f"Already downloaded: {base.repo_relative(output_path(asset))}")
            return
        if job.get("status") in FAILED_STATUSES:
            raise SafeError(f"Prior remesh is {job['status']}; refusing an implicit retry")
        if job.get("submissionStarted") and not job.get("taskId"):
            raise SafeError(
                "Prior remesh submission is uncertain; reconcile Meshy before any retry"
            )

        if not job.get("taskId"):
            balance_before = base.get_balance(key)
            record_balance(session, balance_before)
            snapshot = enforce_budget(session, balance_before)
            job.update(
                balanceBefore=balance_before,
                budgetAccountedBefore=snapshot["accountedSpend"],
                submissionStarted=True,
                submissionStartedAt=base.now_iso(),
                status="SUBMITTING",
            )
            atomic_save(session)

            payload = parameters_for(asset)
            payload["input_task_id"] = source_task_id
            response = base.api_json(key, "remesh", payload)
            task_id = response.get("result")
            if not isinstance(task_id, str) or not task_id:
                atomic_save(session)
                raise SafeError(
                    "Remesh response had no taskId; reconcile Meshy before retrying"
                )
            job["taskId"] = task_id
            job["submittedAt"] = base.now_iso()
            job["status"] = "PENDING"
            atomic_save(session)

            balance_after_post = base.get_balance(key)
            job["balanceAfterPost"] = balance_after_post
            record_balance(session, balance_after_post)
            atomic_save(session)
            print(f"Submitted remesh for {asset}", flush=True)

        poll_and_download(key, session, job, asset)


def dry_run(assets: Iterable[str]) -> None:
    session = load_session()
    snapshot = credit_snapshot(session)
    entries: List[Dict[str, Any]] = []
    for asset in assets:
        _, task_id = source_job(session, asset)
        existing = (session.get("remeshJobs") or {}).get(asset)
        entries.append(
            {
                "asset": asset,
                "sourceJob": ASSETS[asset]["sourceJob"],
                "sourceTaskReady": bool(task_id),
                "parameters": parameters_for(asset),
                "outputPath": base.repo_relative(output_path(asset)),
                "estimatedCredits": ESTIMATED_CREDITS,
                "existingStatus": existing.get("status") if isinstance(existing, dict) else None,
                "submitted": False,
            }
        )
    report = {
        "dryRun": True,
        "sessionPath": base.repo_relative(SESSION_PATH),
        "balanceStart": EXPECTED_BALANCE_START,
        "creditCap": CREDIT_CAP,
        "estimatedCreditsPerSubmission": ESTIMATED_CREDITS,
        "budget": snapshot,
        "assets": entries,
    }
    print(json.dumps(report, ensure_ascii=False, indent=2, sort_keys=True))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("asset", nargs="?", choices=tuple(ASSETS))
    parser.add_argument(
        "--submit",
        action="store_true",
        help="explicitly permit one Remesh POST, or resume that asset",
    )
    args = parser.parse_args()
    if args.submit and not args.asset:
        parser.error("an asset is required with --submit; batch submission is disabled")
    return args


def main() -> int:
    args = parse_args()
    try:
        if not args.submit:
            dry_run((args.asset,) if args.asset else ASSETS.keys())
            return 0
        submit_or_resume(args.asset)
        return 0
    except SafeError as exc:
        print(f"ERROR: {exc}", flush=True)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
