"""Create canonical portable ZIPs for v3.0.20 Windows release QA.

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
REQUIRED = ("VHWuWa.exe", "VHWuWa.Updater.exe", R29_ENTRY,
            "content/r29-release.json", "README_VIET_HOA.txt")

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
        marker = json.loads(z.read("content/r29-release.json"))
        if marker.get("appVersion") != version or marker.get("translationRelease") != "R29":
            raise RuntimeError("Release marker does not match v3.0.20 and R29")
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
    print(f"V320_ZIP_BYTES={output.stat().st_size}")
    print("V320_ZIP_SHA256=" + new_sha)
    print("V320_RELEASE_READY=YES")

if __name__ == "__main__":
    a = argparse.ArgumentParser()
    a.add_argument("stage", type=Path)
    a.add_argument("output", type=Path)
    a.add_argument("--version", default="3.0.20")
    a.add_argument("--expected-files", type=int, default=84)
    args = a.parse_args()
    package(args.stage, args.output, args.version, args.expected_files)
