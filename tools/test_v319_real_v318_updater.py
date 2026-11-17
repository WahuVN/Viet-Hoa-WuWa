#!/usr/bin/env python3
"""Real publicly shipped v3.0.18 updater -> complete v3.0.19 R29 ZIP.

Safe offline verification: --no-relaunch prevents desktop shortcuts, LocalAppData
writes and game changes. Afterwards the compiled migration helper is exercised
with isolated backup paths. The regular startup-health path is separately
covered by scripts/test-client-self-update.ps1.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import time
import zipfile
from pathlib import Path

PUB_HASH = "bc9a896e575d8088595295626503df01b122b9272cae71789ee6a01c0453e2a7"
UPDATER_HASH = "a1903ea4eec2baa94387a22d527b5e758fc5bcb2a7fff5b6a6015c4b386c58a4"
R29_HASH = "38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598"
OLD_HASHES = {
    "content/WuWaVH_EN_99_P.pak": "44350c79cbf9cb307752682f0e0f3a31af983cdc1fea4b24403b7395237df547",
    "content/WuWaVH_HanViet_99_P.pak": "e41177b5860465dfb40fbe2f3fdb4504be1a814a3bfd9bfa4a9da736d0a5e9e2",
    "content/font/WahuFont_100_P.pak": "ad51a1a0d3cc8abc1be43c13576d1b0f1adf2f3bf3b271c10aec836864dd14dc",
    "content/loader/version.dll": "b328db7a57f3fba676b807112432d462ef7fe8c1b49e1572f89a1fbacaec48b3",
    "content/loader/verorg.dll": "cea99f212af557a9613150c5509631403ed0f05f5d9a06ac42039bd59b40e39a",
    "content/loader/WuWaVH.dll": "ba4bcbea10ef98c268683df3f87bd1e5090cf9245eb886d84c84ac82e25e4e5b",
}

def sha256(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as f:
        for b in iter(lambda: f.read(4 * 1024 * 1024), b""):
            h.update(b)
    return h.hexdigest()

def run(cmd: list[str], cwd: Path) -> str:
    r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True,
                       errors="replace", timeout=170)
    output = f"$ {' '.join(cmd[:2])}\nEXIT {r.returncode}\n{r.stdout}\n{r.stderr}\n"
    if r.returncode:
        raise RuntimeError("Command failed:\n" + output[:3000])
    return output

def file_version(path: Path) -> str:
    ps = r"[Diagnostics.FileVersionInfo]::GetVersionInfo($args[0]).FileVersion"
    r = subprocess.run(["powershell.exe", "-NoProfile", "-Command",
                        f"[Diagnostics.FileVersionInfo]::GetVersionInfo('{path}').FileVersion"],
                       capture_output=True, text=True, errors="replace", timeout=25)
    if r.returncode:
        raise RuntimeError(r.stderr)
    return r.stdout.strip()

def main(a: argparse.Namespace) -> int:
    repo = Path(a.repo).resolve()
    public = Path(a.public_zip).resolve()
    final = Path(a.candidate_zip).resolve()
    updater = Path(a.public_updater).resolve()
    output_dir = Path(a.output_dir).resolve()
    sandbox = output_dir / "_REAL_V318_UPGRADE_SANDBOX"
    installed = sandbox / "Installed"
    backups = sandbox / "BackupOutsideApp"
    receipt_file = output_dir / "REAL_318_TO_319_UPGRADE_QA.json"
    log_file = output_dir / "REAL_318_TO_319_UPGRADE_QA.log"

    if sha256(public) != PUB_HASH or sha256(updater) != UPDATER_HASH:
        raise ValueError("v3.0.18 public artifacts do not match published SHA-256.")
    new_zip_hash = sha256(final)
    if sandbox.exists():
        if sandbox.resolve().parent != output_dir:
            raise RuntimeError("Sandbox directory was not within release candidate output.")
        shutil.rmtree(sandbox)
    installed.mkdir(parents=True)
    started = time.monotonic()
    log = []

    with zipfile.ZipFile(public) as z:
        z.extractall(installed)
    assert file_version(installed / "VHWuWa.exe") == "3.0.18.0"
    assert file_version(installed / "VHWuWa.Updater.exe") == "3.0.18.0"

    # Preserve a real user's custom database path, plus two extra files.
    custom_db = installed / "editor" / "project.db"
    custom_db.write_bytes(b"VHWUWA_USER_PROJECT_DB_MUST_SURVIVE_318_TO_319")
    (installed / "user-private-notes.txt").write_text("precious", encoding="utf8")
    (installed / "content" / "my-user-mod.txt").write_text("not app-owned", encoding="utf8")

    # The ORIGINAL 3.0.18 updater is verified byte-for-byte against GitHub.
    # Only its --no-relaunch QA mode is exercised here: this never spawns the
    # production app or alters the user's Desktop/LocalAppData.
    cmd = [str(updater), "--zip", str(final), "--target", str(installed),
           "--expected-version", "3.0.19", "--sha256", new_zip_hash,
           "--no-relaunch"]
    log.append(run(cmd, repo))
    if file_version(installed / "VHWuWa.exe") != "3.0.19.0":
        raise AssertionError("App did not update to 3.0.19.0")
    if file_version(installed / "VHWuWa.Updater.exe") != "3.0.19.0":
        raise AssertionError("Updater did not update to 3.0.19.0")
    if not (installed / "content" / "r29-release.json").exists():
        raise AssertionError("R29 release marker missing")
    if sha256(installed / "content" / "3.8.11" / "WuWaVH_PC_R29_99_P.pak") != R29_HASH:
        raise AssertionError("R29 data hash wrong")
    if custom_db.read_bytes() != b"VHWUWA_USER_PROJECT_DB_MUST_SURVIVE_318_TO_319":
        raise AssertionError("User project DB was overwritten!")
    if not (installed / "editor" / "project.default.db").exists():
        raise AssertionError("Fresh-install default DB missing")
    if (installed / "user-private-notes.txt").read_text(encoding="utf8") != "precious":
        raise AssertionError("Custom file lost")
    avatar_count = len(list((installed / "editor" / "avatars").glob("*.png")))
    if avatar_count < 58:
        raise AssertionError(f"Missing published editor avatars: {avatar_count}")
    if not all((installed / rel).is_file() for rel in OLD_HASHES):
        raise AssertionError("v3.0.18 in-place updater did not retain legacy data for migration")

    # Trigger precisely the production C# migration logic on an isolated tree.
    # It backs up original six content files OUTSIDE the new app dir and removes
    # them only when all six file hashes match public v3.0.18 release.
    log.append(run(["dotnet", "run", "--project",
                    "tests/VHWuWa.ReleaseMigrationQa/VHWuWa.ReleaseMigrationQa.csproj",
                    "-c", "Release", "--", str(installed), str(backups)], repo))
    for relative, expected in OLD_HASHES.items():
        if (installed / relative).exists():
            raise AssertionError(f"Old active content not deactivated: {relative}")
        if sha256(backups / relative) != expected:
            raise AssertionError(f"Backup missing or mismatched: {relative}")
    if (installed / "content" / "my-user-mod.txt").read_text(encoding="utf8") != "not app-owned":
        raise AssertionError("User content removed")
    if custom_db.read_bytes() != b"VHWUWA_USER_PROJECT_DB_MUST_SURVIVE_318_TO_319":
        raise AssertionError("User DB altered by migration")

    receipt = {
        "status": "PASS", "from": "public v3.0.18", "to": "v3.0.19 + R29",
        "usedActualPublicUpdaterSha256": UPDATER_HASH,
        "usedActualPublicZipSha256": PUB_HASH,
        "candidateZipSha256": new_zip_hash,
        "appAndUpdaterVersion": "3.0.19.0",
        "editorAvatarCount": avatar_count,
        "userEditorDbUnmodified": True,
        "userFilesUnmodified": True,
        "sixV318LegacyAssetsBackedUpOutsideApp": True,
        "r29PakHashVerified": True,
        "qaUsedNoRelaunchToAvoidChangingDesktopAndLocalAppData": True,
        "realGuiPromptAndGameApplyNotCoveredByThisTest": True,
        "durationSeconds": round(time.monotonic() - started, 1),
    }
    log_file.write_text("\n".join(log), encoding="utf8")
    receipt_file.write_text(json.dumps(receipt, indent=2, ensure_ascii=False) + "\n",
                            encoding="utf8")
    if not a.keep_sandbox:
        shutil.rmtree(sandbox)
    print(json.dumps(receipt, indent=2, ensure_ascii=False))
    return 0

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--repo", required=True)
    p.add_argument("--public-zip", required=True)
    p.add_argument("--public-updater", required=True)
    p.add_argument("--candidate-zip", required=True)
    p.add_argument("--output-dir", required=True)
    p.add_argument("--keep-sandbox", action="store_true")
    sys.exit(main(p.parse_args()))
