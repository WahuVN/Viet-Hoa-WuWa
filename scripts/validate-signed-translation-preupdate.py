#!/usr/bin/env python3
"""Fail-closed static signature/source preflight for WUWA localized text bundles.

This is NOT a game import, PAK build, runtime QA or release attestation.
It accepts exact signed JSONL cohorts only; it does not infer or approve any beta
or HOLD translation. Output is created in a new directory, never over existing files.
"""
import argparse
import hashlib
import json
import re
import sys
import unicodedata
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

SHA_RE = re.compile(r"^[0-9a-fA-F]{64}$")


def hash_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def read_jsonl(path):
    with path.open(encoding="utf-8-sig") as stream:
        for lineno, line in enumerate(stream, 1):
            if not line.strip():
                continue
            try:
                obj = json.loads(line)
            except ValueError as exc:
                raise ValueError(f"{path}:{lineno}: invalid JSONL: {exc}") from exc
            if not isinstance(obj, dict):
                raise ValueError(f"{path}:{lineno}: expected a JSON object")
            yield obj


def identity(row):
    source = str(row.get("source_json", "")).strip()
    text_id = str(row.get("id", "")).strip()
    if not source or not text_id:
        raise ValueError("missing source_json/id in source or signed input")
    return source, text_id


def load_source(lease_dir):
    files = sorted(lease_dir.rglob("batch_*.jsonl"))
    if not files:
        raise ValueError("no canonical batch_*.jsonl files found")
    result = {}
    for file in files:
        for row in read_jsonl(file):
            key = identity(row)
            if key in result:
                raise ValueError(f"duplicate canonical source key {key}")
            fingerprint = row.get("source_fingerprint")
            if not isinstance(fingerprint, str) or not SHA_RE.fullmatch(fingerprint):
                raise ValueError(f"invalid source fingerprint for {key}")
            result[key] = fingerprint.lower()
    return result, files


def verify_and_merge(source, signed_files):
    merged = {}
    provenance = {}
    for path in signed_files:
        for row in read_jsonl(path):
            key = identity(row)
            if key in merged:
                raise ValueError(f"duplicate signed ID across input cohorts: {key}")
            if key not in source:
                raise ValueError(f"signed ID absent from canonical source: {key}")
            fingerprint = row.get("source_fingerprint")
            if not isinstance(fingerprint, str) or not SHA_RE.fullmatch(fingerprint):
                raise ValueError(f"invalid signed fingerprint: {key}")
            if fingerprint.lower() != source[key]:
                raise ValueError(f"source fingerprint changed for signed ID: {key}")
            vi = row.get("vi")
            if not isinstance(vi, str) or not vi.strip():
                raise ValueError(f"empty VI for signed ID: {key}")
            if "\ufffd" in vi or "\x00" in vi or unicodedata.normalize("NFC", vi) != vi:
                raise ValueError(f"invalid Unicode or non-NFC VI for {key}")
            vi_sha = row.get("vi_sha256")
            if vi_sha is not None:
                calculated = hashlib.sha256(vi.encode("utf-8")).hexdigest()
                if not isinstance(vi_sha, str) or vi_sha.lower() != calculated:
                    raise ValueError(f"invalid signed VI SHA for {key}")
            merged[key] = {
                "source_json": key[0],
                "id": key[1],
                "source_fingerprint": source[key],
                "vi": vi,
                "vi_sha256": hashlib.sha256(vi.encode("utf-8")).hexdigest(),
            }
            provenance[key] = path.name
    return merged, provenance


def main(argv=None):
    parser = argparse.ArgumentParser(description="WUWA v3.0.19 translation signature QA (static only)")
    parser.add_argument("--lease-dir", required=True, type=Path)
    parser.add_argument("--signed", required=True, action="append", type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--expected-lease", type=int, default=None)
    parser.add_argument("--expected-signed", type=int, default=None)
    args = parser.parse_args(argv)

    lease = args.lease_dir.resolve()
    signed = [p.resolve() for p in args.signed]
    out = args.out.resolve()
    if not lease.is_dir():
        raise ValueError("canonical source directory missing")
    if not signed or any(not p.is_file() for p in signed):
        raise ValueError("one or more signed JSONL inputs missing")
    if out.exists():
        raise ValueError("output already exists; refusing overwrite")

    source, batch_files = load_source(lease)
    approved, provenance = verify_and_merge(source, signed)
    if args.expected_lease is not None and len(source) != args.expected_lease:
        raise ValueError(f"lease denominator mismatch: {len(source)} vs {args.expected_lease}")
    if args.expected_signed is not None and len(approved) != args.expected_signed:
        raise ValueError(f"signed expected count mismatch: {len(approved)} vs {args.expected_signed}")
    if len(approved) > len(source):
        raise ValueError("signed count exceeds source lease")

    out.mkdir(parents=True)
    merged_path = out / "SIGNED_ONLY_UNIQUE_STATIC_QA.jsonl"
    with merged_path.open("x", encoding="utf-8", newline="\n") as stream:
        for key in sorted(approved):
            stream.write(json.dumps(approved[key], ensure_ascii=False, separators=(",", ":")) + "\n")
    report = {
        "schema": "WUWA_MOD3019_SIGNED_TRANSLATION_STATIC_PREUPDATE_GATE_V1",
        "checked_utc": datetime.now(timezone.utc).isoformat(),
        "source_lease_count": len(source),
        "source_batch_count": len(batch_files),
        "signed_cohort_counts": dict(Counter(provenance.values())),
        "signed_unique": len(approved),
        "canonical_lease_unsigned": len(source) - len(approved),
        "static_integrity": "PASS",
        "game_version_3_8_verified": False,
        "game_PAK_payload_verified": False,
        "EN_and_HanViet_PAK_both_built": False,
        "font_game_renderer_verified": False,
        "runtime_tested": False,
        "release_allowed": False,
        "signed_only_file": merged_path.name,
        "signed_only_sha256": hash_file(merged_path),
        "source_signed_input_sha256": {p.name: hash_file(p) for p in signed},
        "source_lease_batch_sha256": {p.as_posix(): hash_file(p) for p in batch_files},
        "requirements_remaining": [
            "exact DB triple and redirect classification of all gameplay text",
            "separate EN/HanViet source-authorized merge and fresh PAK unpack",
            "live game build / font / runtime / rollback smoke verification",
            "independent glossary G0 signoff and remaining HOLD resolution"
        ]
    }
    path = out / "STATUS_PREUPDATE_STATIC_QA.json"
    with path.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
    print(json.dumps({
        "result": "PASS_STATIC_ONLY_NOT_RELEASE",
        "lease": len(source),
        "signed": len(approved),
        "out": str(out),
        "report_sha256": hash_file(path)
    }, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, OSError) as exc:
        print(f"FAIL_CLOSED: {exc}", file=sys.stderr)
        sys.exit(2)
