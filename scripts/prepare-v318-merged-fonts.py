#!/usr/bin/env python3
"""Internal v3.0.18 QA: merge 9 verified B119 fonts with existing PC Lexend.

Never edits input packs; no network, game writes, GitHub or Discord actions.
Output must be a new path in a private build directory.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
import tempfile
from pathlib import Path

B119_EXPECTED_SHA256 = "e4d357c971e7b4c8d103042eb9a5f0c0a0fa385653add9733cff23fcdf53f1de"
LAGU = "Client/Content/Aki/UI/Framework/LGUI/Font/LaguSansBold.ufont"


def sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(4 * 1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def asset_files(root: Path) -> dict[str, Path]:
    return {
        p.relative_to(root).as_posix(): p
        for p in root.rglob("*")
        if p.is_file()
    }


def check_font_ink(path: Path, chars: set[str]) -> None:
    from fontTools.ttLib import TTFont
    from fontTools.pens.boundsPen import BoundsPen

    font = TTFont(str(path))
    cmap = font.getBestCmap() or {}
    glyphs = font.getGlyphSet()
    skip = {" ", "\u00a0", "\u00ad", "\u200b", "\u200c", "\u200d", "\u2060"}
    assert font["maxp"].numGlyphs == len(font.getGlyphOrder()) == len(font["hmtx"].metrics)
    if "vmtx" in font:
        assert len(font["vmtx"].metrics) == font["maxp"].numGlyphs
    missing = [f"U+{ord(c):04X}" for c in chars if ord(c) not in cmap]
    if missing:
        raise RuntimeError(f"{path.name}: missing Unicode " + ", ".join(missing[:10]))
    for ch in chars - skip:
        pen = BoundsPen(glyphs)
        glyphs[cmap[ord(ch)]].draw(pen)
        if pen.bounds is None:
            raise RuntimeError(f"{path.name}: empty glyph U+{ord(ch):04X}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", type=Path, required=True, help="Current PC WahuFont_100_P.pak")
    parser.add_argument("--b119", type=Path, required=True, help="Official audited styled B119 PAK")
    parser.add_argument("--charset-a", type=Path, required=True)
    parser.add_argument("--charset-b", type=Path, required=True)
    parser.add_argument("--tools", type=Path, required=True, help="Trusted WAHU wahu_pack scripts directory")
    parser.add_argument("--repak", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True, help="New WahuFont_100_P.pak path")
    parser.add_argument("--report", type=Path, required=True)
    a = parser.parse_args()

    if not a.b119.is_file() or not a.base.is_file() or not a.repak.is_file():
        raise RuntimeError("Missing required original assets or repak.exe")
    if a.output.exists() or a.report.exists():
        raise RuntimeError("Refusing overwrite of existing output/report")
    if sha(a.b119) != B119_EXPECTED_SHA256:
        raise RuntimeError("B119 source SHA-256 differs from verified Android QA artifact")

    sys.path.insert(0, str(a.tools.resolve()))
    import wahu_pack
    from wahu_verify_pak_release_v2 import unpack_v12_pak

    if not wahu_pack.verify_v12(str(a.b119))["valid"]:
        raise RuntimeError("B119 V12 index/footer invalid")
    if not wahu_pack.verify_v12(str(a.base))["valid"]:
        raise RuntimeError("PC font V12 index/footer invalid")

    chars = set(json.loads(a.charset_a.read_text("utf-8"))["chars"])
    chars.update(json.loads(a.charset_b.read_text("utf-8"))["target_chars"])
    if len(chars) != 278:
        raise RuntimeError(f"Unexpected B119 QA character coverage: {len(chars)}")

    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.report.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="wahu-v318-fontmerge-") as td:
        work = Path(td)
        source = work / "b119_source"
        current = work / "pc_original"
        merged = work / "merged"
        verify = work / "roundtrip"
        for d in (source, current, merged, verify):
            d.mkdir()
        unpack_v12_pak(a.b119, source, a.repak)
        unpack_v12_pak(a.base, current, a.repak)
        b119_files = asset_files(source)
        pc_files = asset_files(current)
        if len(b119_files) != 10 or set(pc_files) != {LAGU} or LAGU not in b119_files:
            raise RuntimeError("Unexpected PAK font entry names; cannot merge safely")

        record = {}
        for name, file in sorted(b119_files.items()):
            donor = pc_files[LAGU] if name == LAGU else file
            dest = merged / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(donor, dest)
            if name != LAGU:
                check_font_ink(dest, chars)
            record[name] = {
                "origin": "PC_Lexend_3.0.17" if name == LAGU else "B119_real_ink_phone_QA",
                "size": dest.stat().st_size,
                "sha256": sha(dest),
            }
        if len(record) != 10 or sum(x["origin"] == "B119_real_ink_phone_QA" for x in record.values()) != 9:
            raise RuntimeError("Expected nine patched fonts + one unchanged PC Lexend")

        wahu_pack.build_v12_pak(str(merged), str(a.output), str(a.repak))
        if not wahu_pack.verify_v12(str(a.output))["valid"]:
            raise RuntimeError("New merged V12 pack is invalid")
        unpack_v12_pak(a.output, verify, a.repak)
        extracted = asset_files(verify)
        if set(extracted) != set(record):
            raise RuntimeError("Merged font output has unexpected file entries")
        for name, evidence in record.items():
            if sha(extracted[name]) != evidence["sha256"]:
                raise RuntimeError("Merged PAK changed font bytes: " + name)
            if name != LAGU:
                check_font_ink(extracted[name], chars)
        assert sha(extracted[LAGU]) == sha(pc_files[LAGU])

    report = {
        "status": "STATIC_ROUNDTRIP_PASS_PC_RUNTIME_NOT_TESTED",
        "version_candidate": "3.0.18",
        "source_b119_sha256": sha(a.b119),
        "source_pc_font_sha256": sha(a.base),
        "merged_font_pak_sha256": sha(a.output),
        "merged_font_pak_bytes": a.output.stat().st_size,
        "asset_count": 10,
        "patched_fonts_from_B119": 9,
        "original_PC_Lexend_preserved": True,
        "unicode_real_ink_chars_tested_for_9_fonts": 278,
        "pak_v12_roundtrip_verified": True,
        "runtime_wuwa_PC_verified": False,
        "giant_subtitle_exact_runtime_verified": False,
        "font_assets": record,
    }
    a.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", "utf-8")
    print("V318_FONT_PACK_OK " + json.dumps({
        k: v for k, v in report.items() if k != "font_assets"
    }, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
