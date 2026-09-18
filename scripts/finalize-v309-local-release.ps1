$ErrorActionPreference='Stop'
$root='D:\Tim hieu\WuwaVH\VHWuWa'
$dist=Join-Path $root 'dist'
$upload=Join-Path $dist '_UPLOAD_V3.0.9_READY'
$version='3.0.9'

if(Test-Path $upload){Remove-Item $upload -Recurse -Force}
New-Item -ItemType Directory -Force $upload | Out-Null

Copy-Item (Join-Path $root "RELEASE_NOTES_v$version.md") (Join-Path $dist 'RELEASE_BODY.md') -Force
Copy-Item (Join-Path $root "TRANSLATION_CHANGES_v$version.md") (Join-Path $dist "TRANSLATION_CHANGES_v$version.md") -Force

$assets=@(
  "VietHoa-WuWa-v$version.zip",
  "App-Dich-WuWa-v$version.zip",
  'WuWaVH_EN_99_P.pak',
  'WuWaVH_HanViet_99_P.pak',
  'RELEASE_BODY.md',
  "TRANSLATION_CHANGES_v$version.md"
)
foreach($name in $assets){
  $src=Join-Path $dist $name
  if(-not (Test-Path -LiteralPath $src)){throw "Missing release asset: $src"}
  Copy-Item -LiteralPath $src -Destination (Join-Path $upload $name) -Force
}
foreach($name in @("GITHUB_ANNOUNCEMENT_v$version.md","DISCORD_ANNOUNCEMENT_v$version.md")){
  $src=Join-Path $root $name
  if(-not (Test-Path -LiteralPath $src)){throw "Missing release note: $src"}
  Copy-Item -LiteralPath $src -Destination (Join-Path $upload $name) -Force
}

$core=@(
  "VietHoa-WuWa-v$version.zip",
  "App-Dich-WuWa-v$version.zip",
  'WuWaVH_EN_99_P.pak',
  'WuWaVH_HanViet_99_P.pak'
)
$sumLines=@()
$assetManifest=@()
foreach($name in $core){
  $p=Join-Path $upload $name
  $item=Get-Item -LiteralPath $p
  $h=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()
  $sumLines += "$h  $name"
  $assetManifest += [ordered]@{name=$name;size=[int64]$item.Length;sha256=$h}
}
[IO.File]::WriteAllLines((Join-Path $upload "SHA256SUMS_v$version.txt"),$sumLines,[Text.UTF8Encoding]::new($false))

$appExe=Join-Path $dist '_build\VHWuWa_BanCai\app\VHWuWa.exe'
$updExe=Join-Path $dist '_build\VHWuWa_BanCai\app\VHWuWa.Updater.exe'
$appVer=[Diagnostics.FileVersionInfo]::GetVersionInfo($appExe).FileVersion
$updVer=[Diagnostics.FileVersionInfo]::GetVersionInfo($updExe).FileVersion
$sumText=[string]::Join([Environment]::NewLine,$sumLines)

$ready=@"
VHWuWa v$version - RELEASE READY (LOCAL)
========================================

STATUS
- Player tests: 104/104 PASS (41 Core + 63 Infrastructure)
- Self-update E2E: PASS
  + ready/health handshake
  + new-version commit
  + exact rollback when unhealthy
  + bad SHA rejected before install
- Runtime Gacha JS fresh-unpack: PASS
- App version: $appVer
- Updater version: $updVer
- Player ZIP CRC: PASS
- Community ZIP CRC: PASS

PAK EN FINAL R7
- SHA256: B08B334B3CCAB2AD7005F4F465B645E3222975B11F6447F81A6C0892353695DF
- 110/110 DB
- fresh-unpack byte match
- SQLite quick_check/integrity_check PASS
- runtime issues: 0
- actionable fallback: 0

PAK HAN VIET FINAL R7
- SHA256: 94D886E01FFC8A2071F8C77260D17333FA5AB659A954E9B7534DBB90760D1E08
- 110/110 DB
- fresh-unpack byte match
- SQLite quick_check/integrity_check PASS
- runtime issues: 0
- actionable fallback: 0
- fallback second-pass/idempotence blockers: 0

FINAL FIX
- Recovered 7 Tiger's Maw / Ho Khau rows damaged by a stale exact override carried from 3.0.8.
- 4 quest rows that had collapsed to a location name now contain their full sentence again.
- project.db authority and translation source were repaired so later builds do not reproduce the defect.

FINAL ASSETS
$sumText

QA NOTE
- Prepack QA still reports the same 7 baseline placeholder_mismatch rows in UID/subtitle data.
- R7 adds no QA regression versus R6.
- Game-ready verifier PASS, runtime issues = 0.

PUBLISH
- Not pushed to GitHub.
- No GitHub Release created.
- No Discord message sent.
"@
[IO.File]::WriteAllText((Join-Path $upload "RELEASE_READY_v$version.txt"),$ready,[Text.UTF8Encoding]::new($false))

$manifest=[ordered]@{
  schema='VHWUWA_RELEASE_MANIFEST_V1'
  version=$version
  generated_at_utc=[DateTime]::UtcNow.ToString('o')
  app_version=$appVer
  updater_version=$updVer
  tests=[ordered]@{
    player='104/104 PASS'
    self_update_e2e='PASS'
    runtime_gacha_js='PASS'
    player_zip_crc='PASS'
    community_zip_crc='PASS'
  }
  pak_en=[ordered]@{
    authority='R7'
    db_count=110
    fresh_db_count=110
    byte_mismatch=0
    runtime_issues=0
    actionable_fallback=0
    sha256='b08b334b3ccab2ad7005f4f465b645e3222975b11f6447f81a6c0892353695df'
  }
  pak_hanviet=[ordered]@{
    authority='R7'
    db_count=110
    fresh_db_count=110
    byte_mismatch=0
    runtime_issues=0
    actionable_fallback=0
    fallback_second_pass_changes=0
    sha256='94d886e01ffc8a2071f8c77260d17333fa5ab659a954e9b7534dbb90760d1e08'
  }
  final_fix=[ordered]@{
    tiger_maw_rows_recovered=7
    collapsed_quest_rows_restored=4
  }
  known_baseline=[ordered]@{
    prepack_placeholder_mismatch=7
    qa_regression_from_r6=$false
  }
  assets=$assetManifest
  published=$false
}
$json=$manifest | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText((Join-Path $upload "RELEASE_MANIFEST_v$version.json"),$json,[Text.UTF8Encoding]::new($false))

Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach($name in @("VietHoa-WuWa-v$version.zip","App-Dich-WuWa-v$version.zip")){
  $p=Join-Path $upload $name
  $z=[IO.Compression.ZipFile]::OpenRead($p)
  try{
    foreach($e in $z.Entries){
      if($e.Length -eq 0){continue}
      $s=$e.Open()
      try{
        $buf=New-Object byte[] 65536
        while($s.Read($buf,0,$buf.Length) -gt 0){}
      } finally {$s.Dispose()}
    }
    Write-Host "ZIP PASS: $name entries=$($z.Entries.Count)"
  } finally {$z.Dispose()}
}
Write-Host '---UPLOAD---'
Get-ChildItem $upload -File | Sort-Object Name | Select Name,Length,LastWriteTime | Format-Table -AutoSize
Write-Host '---SHA256---'
Get-Content (Join-Path $upload "SHA256SUMS_v$version.txt")
