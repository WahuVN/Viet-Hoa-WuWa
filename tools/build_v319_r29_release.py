#!/usr/bin/env python3
"""Build a complete v3.0.19 portable release candidate (R29 game data).

Inputs are locally verified public v3.0.18 ZIP, R29 QA ZIP, and fresh
3.0.19 self-contained app/updater EXEs. This never uploads to GitHub,
installs a game mod, or modifies the user's installed application.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

V318_PUBLIC_SHA256 = "bc9a896e575d8088595295626503df01b122b9272cae71789ee6a01c0453e2a7"
R29_QA_SHA256 = "f0f137d354a47f20a06eb260c8435743de85d276358e647380076dca1ff4aa03"
R29_PAK_SHA256 = "38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598"
R29_PAK_REL = "content/3.8.11/WuWaVH_PC_R29_99_P.pak"
ZIP_NAME = "VietHoa-WuWa-v3.0.19.zip"
LEGACY_CONTENT = {
    "content/WuWaVH_EN_99_P.pak",
    "content/WuWaVH_HanViet_99_P.pak",
    "content/font/WahuFont_100_P.pak",
    "content/loader/version.dll",
    "content/loader/verorg.dll",
    "content/loader/WuWaVH.dll",
}

def sha256(path: Path) -> str:
    with path.open("rb") as inp:
        h = hashlib.sha256()
        for chunk in iter(lambda: inp.read(4 * 1024 * 1024), b""):
            h.update(chunk)
        return h.hexdigest()

def verified_input(path: Path, expected_sha256: str) -> None:
    if not path.is_file():
        raise FileNotFoundError(path)
    got = sha256(path)
    if got.lower() != expected_sha256.lower():
        raise ValueError(f"SHA-256 mismatch: {path} expected={expected_sha256} got={got}")

def safe_member(name: str) -> Path:
    if not name or name.startswith("/") or "\\" in name or ":" in name:
        raise ValueError(f"Unsafe ZIP path: {name!r}")
    parts = PurePosixPath(name).parts
    if ".." in parts or "." in parts:
        raise ValueError(f"Unsafe ZIP path: {name!r}")
    return Path(*parts)

def extract_qa_archive(zip_path: Path, stage: Path) -> int:
    seen: set[str] = set()
    files = 0
    with zipfile.ZipFile(zip_path) as z:
        for entry in z.infolist():
            if entry.is_dir():
                continue
            rel = safe_member(entry.filename)
            key = rel.as_posix().lower()
            if key in seen:
                raise ValueError(f"Duplicate ZIP path: {entry.filename!r}")
            seen.add(key)
            if rel.as_posix() == "QA_R29_MANIFEST.json":
                continue  # private QA marker, not a public release asset
            dest = stage / rel
            dest.parent.mkdir(parents=True, exist_ok=True)
            with z.open(entry) as src, dest.open("wb") as out:
                shutil.copyfileobj(src, out, length=1024 * 1024)
            files += 1
    return files

def inspect_legacy_editor(zip_path: Path, stage: Path) -> tuple[int, int]:
    """Verify all shipped character images, reject obsolete 3.4-3.7 build sources.

    The 482 editor files absent from R29 are old TextSource/build_support files,
    NOT character images. Shipping those can point the editor at outdated data.
    """
    with zipfile.ZipFile(zip_path) as z:
        names = {entry.filename for entry in z.infolist() if not entry.is_dir()}
    public_avatars = {n for n in names if n.startswith("editor/avatars/") and n.endswith(".png")}
    stage_avatars = {p.relative_to(stage).as_posix()
                     for p in (stage / "editor" / "avatars").glob("*.png")}
    if public_avatars - stage_avatars:
        raise ValueError(f"R29 QA is missing user-facing avatars: {sorted(public_avatars-stage_avatars)[:5]}")
    obsolete_build_support = [n for n in names if n.startswith("editor/build_support/")]
    if len(obsolete_build_support) != 482:
        raise ValueError(f"Unexpected public legacy build-support inventory: {len(obsolete_build_support)}")
    return len(stage_avatars), len(obsolete_build_support)

def prepare(args: argparse.Namespace) -> int:
    public_zip = Path(args.public_v318).resolve()
    qa_zip = Path(args.r29_qa_zip).resolve()
    app = Path(args.app_exe).resolve()
    updater = Path(args.updater_exe).resolve()
    out_dir = Path(args.output_dir).resolve()
    stage = out_dir / "APP_PORTABLE"
    package = out_dir / ZIP_NAME

    verified_input(public_zip, V318_PUBLIC_SHA256)
    verified_input(qa_zip, R29_QA_SHA256)
    if not app.is_file() or not updater.is_file():
        raise FileNotFoundError("Fresh 3.0.19 app/updater publish EXEs required.")

    if stage.exists():
        shutil.rmtree(stage)
    stage.mkdir(parents=True)
    qa_files = extract_qa_archive(qa_zip, stage)
    avatars_validated, obsolete_support_excluded = inspect_legacy_editor(public_zip, stage)

    # The old R29 QA ZIP may contain v3.0.16 help pages. Always package the
    # current, reviewed guides from this source checkout with the new EXE.
    guides_source = Path(__file__).resolve().parents[1] / "Guides" / "vi-VN"
    if not guides_source.is_dir():
        raise FileNotFoundError("Missing source Guides/vi-VN")
    for guide in sorted(guides_source.glob("*.md")):
        shutil.copy2(guide, stage / "Guides" / "vi-VN" / guide.name)
    guides = sorted((stage / "Guides" / "vi-VN").glob("*.md"))
    if len(guides) < 10:
        raise ValueError(f"Expected ten up-to-date in-app help topics; only {len(guides)} found")
    if any("Bản 3.0.16 xử lý tự động" in guide.read_text(encoding="utf-8-sig")
           for guide in guides):
        raise ValueError("Stale v3.0.16 help content would be shipped")

    # Always replace the QA EXEs with the two freshly built v3.0.19 binaries.
    shutil.copy2(app, stage / "VHWuWa.exe")
    shutil.copy2(updater, stage / "VHWuWa.Updater.exe")
    # The editor database is mutable user data. An in-place updater overlays ZIP
    # entries, so never ship project.db at the same path as the live user database.
    project = stage / "editor" / "project.db"
    default_project = stage / "editor" / "project.default.db"
    if not project.exists() or default_project.exists():
        raise ValueError("Unexpected editor project database layout")
    project.replace(default_project)
    r29 = stage / R29_PAK_REL
    verified_input(r29, R29_PAK_SHA256)

    marker = {
        "appVersion": "3.0.19",
        "translationRelease": "R29",
        "resourceVersion": "3.8.11",
        "supportedGame": "Wuthering Waves PC 3.8.0 / 3.8.11 (MAY-007 verified build only)",
        "pakSha256": R29_PAK_SHA256,
        "pakPath": R29_PAK_REL,
        "mode": "mount-sidecar-no-dll",
        "hanVietSupported": False,
        "separateFontPakSupported": False,
        "note": "If R29 is absent or invalid, never fall back to old v3.7 DLL/PAK.",
    }
    marker_path = stage / "content" / "r29-release.json"
    marker_path.write_text(json.dumps(marker, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    (stage / "README_VIET_HOA.txt").write_text(
        "WuWa Viet Hoa PC v3.0.19 - R29\n"
        "1. Giai nen toan bo ZIP vao mot thu muc co dinh.\n"
        "2. Chay VHWuWa.exe (khong mo truc tiep tu ZIP).\n"
        "3. Chon thu muc game. R29 chi ho tro ban PC 3.8.0 / data 3.8.11\n"
        "   va file game da xac minh; neu khong khop, nut Cai se bi khoa.\n"
        "4. R29 giu ten nhan vat quoc te; chua ho tro Han Viet / font PAK rieng.\n"
        "5. Ung dung hoi truoc khi tai/cap nhat tren GitHub; co rollback.\n"
        "6. Da tai tu Github Releases chinh thuc; khong tat Defender/SmartScreen.\n",
        encoding="utf-8",
    )

    all_files = sorted(p for p in stage.rglob("*") if p.is_file())
    rel_names = {p.relative_to(stage).as_posix() for p in all_files}
    required = {
        "VHWuWa.exe", "VHWuWa.Updater.exe", R29_PAK_REL,
        "content/r29-release.json", "editor/WahuPakEditor.exe",
        "editor/project.default.db", "Fonts/Lexend-Variable.ttf", "tools/font-weight-instance.exe",
        "Guides/vi-VN/01-bat-dau.md", "Guides/vi-VN/10-ho-tro.md",
    }
    missing = required - rel_names
    if missing:
        raise ValueError(f"Release missing files: {sorted(missing)}")
    if rel_names & LEGACY_CONTENT:
        raise ValueError(f"Obsolete v3.7 PAK/loader was accidentally shipped: {rel_names & LEGACY_CONTENT}")
    if "editor/project.db" in rel_names:
        raise ValueError("Update ZIP would overwrite a user's mutable editor/project.db")
    avatars = [n for n in rel_names if n.startswith("editor/avatars/") and n.endswith(".png")]
    if len(avatars) < 58 or len(rel_names) < 80:
        raise ValueError(f"Incomplete R29 runtime release: {len(rel_names)} total files, {len(avatars)} avatars")
    if any(n.startswith("editor/build_support/") for n in rel_names):
        raise ValueError("Obsolete 3.4-3.7 build-support should not be shipped in R29")

    if package.exists():
        package.unlink()
    with zipfile.ZipFile(package, "w", zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as z:
        for file in all_files:
            z.write(file, arcname=file.relative_to(stage).as_posix())
    with zipfile.ZipFile(package) as z:
        checked = {e.filename for e in z.infolist() if not e.is_dir()}
        if checked != rel_names:
            raise RuntimeError("Round-trip ZIP file list mismatch")
        for required_name in required:
            if required_name not in checked:
                raise RuntimeError(f"ZIP missing {required_name}")
    receipt = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "state": "LOCAL_QA_ONLY",
        "version": "3.0.19",
        "translation": "R29",
        "sha256": sha256(package),
        "archiveBytes": package.stat().st_size,
        "totalFiles": len(rel_names),
        "newQaFiles": qa_files,
        "currentHelpTopicsBundled": len(guides),
        "legacyHelp316Rejected": True,
        "publicV318AvatarsValidatedPresent": avatars_validated,
        "totalEditorAvatars": len(avatars),
        "obsoleteEditorBuildSupportFilesIntentionallyExcluded": obsolete_support_excluded,
        "pakSha256": R29_PAK_SHA256,
        "inputs": {
            "public318Sha256": V318_PUBLIC_SHA256,
            "r29QaSha256": R29_QA_SHA256,
            "appExeSha256": sha256(app),
            "updaterExeSha256": sha256(updater),
        },
        "oldPakAndLoaderNotShipped": True,
        "userEditorProjectNeverOverwritten": True,
        "userConfigurationPath": "%LOCALAPPDATA%/VHWuWa",
        "readyToPublish": False,
    }
    (out_dir / "QA_RELEASE_RECEIPT.json").write_text(
        json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (out_dir / "SHA256SUMS.txt").write_text(
        receipt["sha256"] + " *" + ZIP_NAME + "\n", encoding="ascii")
    print(json.dumps({"zip": str(package), **receipt}, ensure_ascii=False, indent=2))
    return 0

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--public-v318", required=True)
    parser.add_argument("--r29-qa-zip", required=True)
    parser.add_argument("--app-exe", required=True)
    parser.add_argument("--updater-exe", required=True)
    parser.add_argument("--output-dir", required=True)
    sys.exit(prepare(parser.parse_args()))
