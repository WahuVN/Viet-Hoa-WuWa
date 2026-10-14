#!/usr/bin/env python3
"""Verify exact GitHub v3.0.17 assets for the already-shipped PC v3.0.16 updater.
No publication, no installer execution, no game modification.
"""
from pathlib import Path
import hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[1]
stage=ROOT/"dist/release-prep/v3.0.17"
rc3=json.loads((ROOT/"dist/VietHoa-WuWa-v3.0.17-RC3-UI-TextQA.json").read_text("utf-8-sig"))
rc2=json.loads((ROOT/"dist/VietHoa-WuWa-v3.0.17-RC2-TextQA.json").read_text("utf-8-sig"))
updater=json.loads((ROOT/"dist/VietHoa-WuWa-v3.0.17-RC3-UPGRADE-AUDIT.json").read_text("utf-8-sig"))
expected={
 "VietHoa-WuWa-v3.0.17.zip":rc3["zip_sha256"],
 "WuWaVH_EN_99_P.pak":rc2["pak_sha256"]["WuWaVH_EN_99_P.pak"],
 "WuWaVH_HanViet_99_P.pak":rc2["pak_sha256"]["WuWaVH_HanViet_99_P.pak"]
}
assert updater["actual_package_applied"] and updater["sha_rejected_before_modification"]
assert updater["config_preserved"] and updater["zip_sha256"] == rc3["zip_sha256"]
assert rc3["zip_crc"]=="FULL_PASS" and rc3["version"]=="3.0.17"
hashes={}
for name,expect in expected.items():
 path=stage/name
 assert path.is_file(), f"MISSING {path}"
 digest=hashlib.sha256()
 with path.open("rb") as stream:
  for chunk in iter(lambda:stream.read(4*1024*1024),b""):
   digest.update(chunk)
 actual=digest.hexdigest()
 assert actual==expect, f"SHA FAIL {name}: {actual} != {expect}"
 hashes[name]=actual
 print("ASSET_SHA_PASS",name,path.stat().st_size,actual,flush=True)
archive=stage/"VietHoa-WuWa-v3.0.17.zip"
with zipfile.ZipFile(archive,"r") as z:
 names={entry.filename for entry in z.infolist()}
 for required in ("VHWuWa.exe","VHWuWa.Updater.exe","content/WuWaVH_EN_99_P.pak",
   "content/WuWaVH_HanViet_99_P.pak","content/font/WahuFont_100_P.pak"):
  assert required in names,f"MISSING FROM ZIP {required}"
 assert len(names)==rc3["zip_entries"],f"ZIP size mismatch {len(names)}"
 assert not any(n.startswith("/") or ".." in Path(n).parts for n in names),"ZIP unsafe path"
 print("ZIP_MANIFEST_PASS",len(names),flush=True)
note=stage/"SHA256SUMS.txt"
note.write_text("".join(f"{sha}  {name}\n" for name,sha in hashes.items()),encoding="utf-8")
result={"status":"READY_TO_UPLOAD_DRAFT_NOT_PUBLIC","tag":"v3.0.17",
 "assets":[{"name":n,"bytes":(stage/n).stat().st_size,"sha256":s} for n,s in hashes.items()],
 "updater_3_0_16_to_3_0_17":"PASS_REAL_OLD_UPDATER",
 "github_release_latest_online":"NOT_TESTED_UNTIL_PUBLISHED",
 "game_runtime_text_font":"HOLD",
 "release_requires_published_not_draft_not_prerelease":True}
(stage/"RELEASE_PREFLIGHT.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
print("RELEASE_PREFLIGHT_PASS",len(hashes),"ASSETS",flush=True)
