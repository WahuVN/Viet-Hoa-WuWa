#!/usr/bin/env python3
"""Fail-closed release QA: verify stage and ZIP byte equality, paths and R29 media."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

EXPECT = {
    "content/3.8.11/WuWaVH_PC_R29_99_P.pak":
        "38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598",
    "Assets/bg-video.mp4":
        "022703c38d9d0d01e1106c959b7afc8c87bcdd6401aead77bb2e2d48b0fdf8d6",
    "Fonts/Lexend-660.ttf":
        "48a4e2d6cc06cc403a904279b88d9c5275ef304c66d300126cca2489ca7be168",
    "content/font/Lexend-660_100_P.pak":
        "6be60ffc39ce0ee0a31e7dfbda0acc9e243f395ef94a65609baa8146ba62ca68",
}
REQUIRED = (
    "VHWuWa.exe", "VHWuWa.Updater.exe", "content/r29-release.json",
    "Fonts/fonts.json", "Config/game.json", "editor/WahuPakEditor.exe",
    "tools/repak.exe", "tools/font-weight-instance.exe",
    "README_VIET_HOA.txt",
) + tuple(EXPECT)

def digest(stream):
    h = hashlib.sha256()
    while data := stream.read(4 << 20):
        h.update(data)
    return h.hexdigest()

def verify(stage: Path, archive_path: Path, version: str):
    if not stage.is_dir() or not archive_path.is_file():
        raise RuntimeError("Missing stage or release ZIP.")
    files = sorted((p for p in stage.rglob("*") if p.is_file()),
                   key=lambda p: p.relative_to(stage).as_posix().lower())
    expected = {p.relative_to(stage).as_posix(): p for p in files}
    if len(expected) != len(files):
        raise RuntimeError("Duplicate stage paths.")
    with zipfile.ZipFile(archive_path, "r") as z:
        names = [a.filename for a in z.infolist() if not a.is_dir()]
        if len(names) != len(files) or set(names) != set(expected):
            raise RuntimeError(f"ZIP stage mismatch (zip={len(names)}, stage={len(files)})")
        if len({n.lower() for n in names}) != len(names):
            raise RuntimeError("Duplicate Windows paths in ZIP.")
        if any(n.startswith("/") or ".." in n.split("/") or
               "\\" in n or ":" in n for n in names):
            raise RuntimeError("Unsafe path in ZIP.")
        for name in REQUIRED:
            if name not in names:
                raise RuntimeError(f"Required file absent: {name}")
        total = 0
        for name in names:
            entry = z.getinfo(name)
            stage_file = expected[name]
            if entry.file_size != stage_file.stat().st_size:
                raise RuntimeError(f"File length mismatch: {name}")
            with z.open(entry) as compressed, stage_file.open("rb") as original:
                if digest(compressed) != digest(original):
                    raise RuntimeError(f"Byte mismatch: {name}")
            total += entry.file_size
        for name, sha in EXPECT.items():
            with z.open(name) as inp:
                if digest(inp) != sha:
                    raise RuntimeError(f"Pinned hash mismatch: {name}")
        release = json.loads(z.read("content/r29-release.json"))
        if release.get("appVersion") != version or release.get("translationRelease") != "R29":
            raise RuntimeError("Release marker version/content mismatch.")
        fonts = json.loads(z.read("Fonts/fonts.json"))
        if fonts.get("count") != 73 or len(fonts.get("fonts", [])) != 73:
            raise RuntimeError("73-font catalog mismatch.")
    print(f"VERIFIED_ZIP_FILES={len(files)}")
    print(f"VERIFIED_UNCOMPRESSED_BYTES={total}")
    print("R29_FONTS_BG_VIDEO_CATALOG=PASS")
    print("ZIP_BYTE_IDENTICAL_TO_STAGE=PASS")
    with archive_path.open("rb") as f:
        print(f"ZIP_SHA256={digest(f)}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--stage", required=True, type=Path)
    parser.add_argument("--zip", required=True, type=Path)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    verify(args.stage, args.zip, args.version)
