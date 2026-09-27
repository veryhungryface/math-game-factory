"""Build a compact, non-scoring index over final round-3 evidence."""
from pathlib import Path
import datetime
import gzip
import hashlib
import json

root = Path.cwd()
here = Path(__file__).resolve().parent
game = root / "public/g/hyeopgok-sasu"


def read(path):
    return json.loads(path.read_text())


capture = read(here / "final/report.json")
qa = read(here / "final/qa/report.json")
packs = read(here / "final/packs/report.json")
economy = read(here / "economy.json")
regression = read(here / "final-regression.json")

files = []
for path in sorted(game.rglob("*")):
    if not path.is_file() or path.name.startswith("."):
        continue
    payload = path.read_bytes()
    files.append(
        {
            "file": str(path.relative_to(game)),
            "rawBytes": len(payload),
            "gzipBytes": len(gzip.compress(payload, compresslevel=9, mtime=0)),
            "sha256": hashlib.sha256(payload).hexdigest(),
        }
    )

steady = [m for m in capture["metrics"] if m["label"].endswith("steady15s")]
summary = {
    "time": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "status": "Local art round 3 complete; independent art verdict pending; no push or deploy",
    "build": capture["build"],
    "qa": {key: qa.get(key) for key in ("passed", "total", "failed", "fatal", "auto_pass", "render", "perf")},
    "qaNonfatal": [check for check in qa.get("checks", []) if not check.get("ok") and not check.get("fatal")],
    "drawsMaximum": max(m["draws"]["max"] for m in capture["metrics"]),
    "steadyAllocationZero": all(m["managedAllocatedBytes"]["totals"][:3] == [0, 0, 0] for m in steady),
    "allocationScope": "15 steady seconds after warmup; Game.Update, Battle.Update, Environment.LateUpdate",
    "steady": steady,
    "gzipBytes": sum(item["gzipBytes"] for item in files),
    "rawBytes": sum(item["rawBytes"] for item in files),
    "files": files,
    "captureErrors": capture["errors"],
    "packCapture": {
        "frames": len(packs["frames"]),
        "errors": packs["errors"],
        "fontWarnings": packs["warnings"],
        "packsUnchanged": packs["packsUnchanged"],
        "buildMatches": packs["build"] == capture["build"],
    },
    "economy": {
        "pass": economy["pass"],
        "cases": len(economy["cases"]),
        "checks": len(economy["checks"]),
        "buildMatches": economy["buildHashes"] == capture["build"],
    },
    "regression": {
        "pass": regression["pass"],
        "checks": len(regression["checks"]),
        "errors": regression["errors"],
        "failedRequests": regression["failedRequests"],
        "buildMatches": regression["buildHashes"] == capture["build"],
    },
}
(here / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n")
print(
    json.dumps(
        {key: summary[key] for key in ("qa", "drawsMaximum", "steadyAllocationZero", "gzipBytes", "packCapture", "economy", "regression")},
        ensure_ascii=False,
        indent=2,
    )
)
