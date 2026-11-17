"""Create canonical portable ZIPs for the current Windows release QA.

Preserve directory structure with forward-slash ZIP entries, verify the R29 PAK
inside the archive and only replace an existing candidate after all checks pass.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import zipfile

R29_SHA256 = "38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598"
R29_ENTRY = "content/3.8.11/WuWaVH_PC_R29_99_P.pak"
MEDIA_SHA256 = "022703c38d9d0d01e1106c959b7afc8c87bcdd6401aead77bb2e2d48b0fdf8d6"
FONT660_SHA256 = "48a4e2d6cc06cc403a904279b88d9c5275ef304c66d300126cca2489ca7be168"
FONT660_PAK_SHA256 = "6be60ffc39ce0ee0a31e7dfbda0acc9e243f395ef94a65609baa8146ba62ca68"
REQUIRED = ("VHWuWa.exe", "VHWuWa.Updater.exe", R29_ENTRY,
            "content/r29-release.json", "README_VIET_HOA.txt",
            "Assets/bg-video.mp4", "Fonts/Lexend-660.ttf", "Fonts/fonts.json",
            "content/font/Lexend-660_100_P.pak",
            "Config/game.json", "editor/WahuPakEditor.exe",
            "tools/font-weight-instance.exe", "tools/repak.exe")

def digest_stream(stream):
    h = hashlib.sha256()
    while True:
        data = stream.read(4 * 1024 * 1024)
        if not data:
            break
        h.update(data)
    return h.hexdigest()

def package(stage: Path, output: Path, version: str, expected_files: int):
    stage = stage.resolve()
    output = output.resolve()
    files = sorted((p for p in stage.rglob("*") if p.is_file()), key=lambda p: str(p).lower())
    if len(files) != expected_files:
        raise RuntimeError(f"Expected {expected_files} stage files; found {len(files)}")
    names = [p.relative_to(stage).as_posix() for p in files]
    if len(set(n.casefold() for n in names)) != len(names) or any("../" in n or n.startswith("/") or ":" in n for n in names):
        raise RuntimeError("Duplicate or unsafe ZIP path")
    missing = set(REQUIRED) - set(names)
    if missing:
        raise RuntimeError(f"Missing required release assets: {missing}")
    output.parent.mkdir(parents=True, exist_ok=True)
    temp = output.with_name(output.name + ".normalized")
    if temp.exists():
        if temp.stat().st_size != 0:
            raise RuntimeError(f"Refusing to overwrite existing archive temp: {temp}")
        temp.unlink()
    with zipfile.ZipFile(temp, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as z:
        for path, name in zip(files, names):
            if path.is_symlink():
                raise RuntimeError(f"Symlink not supported: {path}")
            z.write(path, arcname=name)
    with zipfile.ZipFile(temp, "r") as z:
        actual = [f.filename for f in z.infolist() if not f.is_dir()]
        if actual != names or any("\\" in name for name in actual):
            raise RuntimeError("Canonical paths or content changed")
        if z.getinfo(R29_ENTRY).file_size != 125532999:
            raise RuntimeError("R29 size incorrect in ZIP")
        with z.open(R29_ENTRY) as pak_stream:
            pak_sha = digest_stream(pak_stream)
        if pak_sha != R29_SHA256:
            raise RuntimeError(f"R29 SHA inside ZIP mismatch: {pak_sha}")
        for name, required_sha in (("Assets/bg-video.mp4", MEDIA_SHA256),
                                   ("Fonts/Lexend-660.ttf", FONT660_SHA256),
                                   ("content/font/Lexend-660_100_P.pak", FONT660_PAK_SHA256)):
            with z.open(name) as stream:
                actual_sha = digest_stream(stream)
            if actual_sha != required_sha:
                raise RuntimeError(f"Source asset mismatch {name}: {actual_sha}")
        fonts_catalog = json.loads(z.read("Fonts/fonts.json"))
        if fonts_catalog.get("count") != 73 or len(fonts_catalog.get("fonts", [])) != 73:
            raise RuntimeError("Required 73-font catalog missing or incomplete")
        marker = json.loads(z.read("content/r29-release.json"))
        if marker.get("appVersion") != version or marker.get("translationRelease") != "R29":
            raise RuntimeError("Release marker does not match the expected version and R29")
        if any(z.getinfo(x).file_size == 0 for x in REQUIRED):
            raise RuntimeError("Empty required release entry")
    with temp.open("rb") as archive_stream:
        new_sha = digest_stream(archive_stream)
    prior = output.with_name(output.name + ".preliminary")
    if prior.exists():
        raise RuntimeError(f"Refusing to replace existing backup: {prior}")
    if output.exists():
        os.replace(output, prior)
    try:
        os.replace(temp, output)
    except Exception:
        if prior.exists():
            os.replace(prior, output)
        raise
    if prior.exists():
        prior.unlink()
    checksums = output.parent / "SHA256SUMS.txt"
    checksums.write_text(f"{new_sha}  {output.name}\n", encoding="utf-8")
    print(f"ZIP_FILES={len(names)}")
    print("CANONICAL_ZIP_PATHS=PASS")
    print("R29_ARCHIVE_SHA256=" + pak_sha)
    print(f"RELEASE_ZIP_BYTES={output.stat().st_size}")
    print("RELEASE_ZIP_SHA256=" + new_sha)
    print("RELEASE_RELEASE_READY=YES")

if __name__ == "__main__":
    a = argparse.ArgumentParser()
    a.add_argument("stage", type=Path)
    a.add_argument("output", type=Path)
    a.add_argument("--version", required=True)
    a.add_argument("--expected-files", type=int, required=True)
    args = a.parse_args()
    package(args.stage, args.output, args.version, args.expected_files)
