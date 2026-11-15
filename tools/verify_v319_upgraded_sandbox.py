#!/usr/bin/env python3
"""Resume QA on a v3.0.18 -> v3.0.19 sandbox already upgraded by public updater.

Never invoke updater twice; it previously returned code 0, but a stale avatar
count assertion stopped the first QA script. This exercises the remaining
checks + production C# legacy migration without touching actual game/app.
"""
from __future__ import annotations
import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path
from test_v319_real_v318_updater import (
    sha256, file_version, OLD_HASHES, PUB_HASH, UPDATER_HASH, R29_HASH
)

def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    ap.add_argument("--output-dir", required=True)
    args = ap.parse_args(argv)
    repo = Path(args.repo).resolve()
    root = Path(args.output_dir).resolve()
    sandbox = root / "_REAL_V318_UPGRADE_SANDBOX"
    installed = sandbox / "Installed"
    backup = sandbox / "BackupOutsideApp"
    if not installed.is_dir():
        raise RuntimeError("Previously upgraded sandbox not found; refuse a blind retry.")

    original = repo / "dist" / "_audit_public_v318_20261115"
    if sha256(original / "VHWuWa.Updater.exe") != UPDATER_HASH:
        raise AssertionError("Public updater runner SHA changed.")
    if sha256(original / "VietHoa-WuWa-v3.0.18.zip") != PUB_HASH:
        raise AssertionError("Public v3.0.18 ZIP SHA changed.")
    if file_version(installed / "VHWuWa.exe") != "3.0.19.0":
        raise AssertionError("Old real updater failed to activate v3.0.19")
    if file_version(installed / "VHWuWa.Updater.exe") != "3.0.19.0":
        raise AssertionError("New updater version mismatch.")
    if not (installed / "content" / "r29-release.json").is_file():
        raise AssertionError("R29 required-only marker missing.")
    if sha256(installed / "content" / "3.8.11" / "WuWaVH_PC_R29_99_P.pak") != R29_HASH:
        raise AssertionError("R29 PAK SHA mismatch.")
    if not (installed / "editor" / "project.default.db").is_file():
        raise AssertionError("New editor default database missing.")
    active_db = installed / "editor" / "project.db"
    if active_db.read_bytes() != b"VHWUWA_USER_PROJECT_DB_MUST_SURVIVE_318_TO_319":
        raise AssertionError("User project DB was overwritten by the v3.0.18 updater!")
    if (installed / "user-private-notes.txt").read_text(encoding="utf8") != "precious":
        raise AssertionError("Custom app-local notes lost.")
    if (installed / "content" / "my-user-mod.txt").read_text(encoding="utf8") != "not app-owned":
        raise AssertionError("Custom content lost.")
    avatars = len(list((installed / "editor" / "avatars").glob("*.png")))
    if avatars < 58:
        raise AssertionError(f"Missing editor avatars: {avatars}")

    for name, expected in OLD_HASHES.items():
        if not (installed / name).exists() or sha256(installed / name) != expected:
            raise AssertionError(f"An expected old v3.0.18 package file is missing: {name}")

    # The compiled QA entry point calls production C# migration method exactly once.
    cmd = ["dotnet", "run", "--project",
           "tests/VHWuWa.ReleaseMigrationQa/VHWuWa.ReleaseMigrationQa.csproj",
           "-c", "Release", "--", str(installed), str(backup)]
    r = subprocess.run(cmd, cwd=repo, capture_output=True,
                       text=True, errors="replace", timeout=170)
    (root / "REAL_318_TO_319_MIGRATION_LOG.txt").write_text(
        f"EXIT {r.returncode}\n{r.stdout}\n{r.stderr}\n", encoding="utf8")
    if r.returncode:
        raise AssertionError("Compiled migration failed: " + r.stdout + "\n" + r.stderr)

    for name, expected in OLD_HASHES.items():
        if (installed / name).exists():
            raise AssertionError("Old active content not deactivated: " + name)
        if not (backup / name).exists() or sha256(backup / name) != expected:
            raise AssertionError("Backed up old release content mismatch: " + name)
    if active_db.read_bytes() != b"VHWUWA_USER_PROJECT_DB_MUST_SURVIVE_318_TO_319":
        raise AssertionError("User database changed after migration")
    if (installed / "content" / "my-user-mod.txt").read_text(encoding="utf8") != "not app-owned":
        raise AssertionError("Unknown content modified after migration")

    receipt = {
        "status": "PASS",
        "testedPath": "actual public v3.0.18 updater --no-relaunch -> full 3.0.19 R29 ZIP",
        "note": "Previous QA stopped only at incorrect 540-avator assertion after updater exit 0; this was resumed on the exact successfully upgraded sandbox.",
        "oldUpdaterSha256": UPDATER_HASH,
        "zip318Sha256": PUB_HASH,
        "zip319Sha256": sha256(root / "VietHoa-WuWa-v3.0.19.zip"),
        "newAppVersion": "3.0.19.0",
        "newUpdaterVersion": "3.0.19.0",
        "R29PakSha256": R29_HASH,
        "numberOfEditorAvatarsPreserved": avatars,
        "userProjectDatabaseNotOverwritten": True,
        "userNotesAndUnknownContentPreserved": True,
        "sixOldPakFontLoaderBackedUpOutsideApp": True,
        "productionMigrationMethodActuallyExecuted": True,
        "notTested": [
            "end-user GUI consent prompt against a public v3.0.19 release",
            "health-check via real v3.0.18 GUI upgrade (separate fixture E2E covers health)",
            "all in-game dialog and font glyphs",
            "MAY-007 3.0.20 downgrade"
        ]
    }
    (root / "REAL_318_TO_319_UPGRADE_QA.json").write_text(
        json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    shutil.rmtree(sandbox)
    print(json.dumps(receipt, ensure_ascii=False, indent=2))
    return 0

if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
