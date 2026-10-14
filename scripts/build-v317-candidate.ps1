# Bản đóng gói 3.0.17 từ source hiện hành + PAK EN/HV baseline 3.0.16.
# Chỉ dùng QA nội bộ. Không tạo GitHub release hay sửa BotDis production.
param([string]$Version = "3.0.17")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$seed = Join-Path $root "dist\VietHoa-WuWa-v3.0.16-AUTO-CLEAN-LEXEND-QA-20261013"
$fontPak = Join-Path $root "build\v317-font\WahuFont_100_P.pak"
$workspace = Join-Path $root "build\v317-candidate"
$stage = Join-Path $workspace "app"
$updater = Join-Path $workspace "updater"
$zipPath = Join-Path $root "dist\VietHoa-WuWa-v$Version-RC1-QA.zip"
if ($Version -ne "3.0.17") { throw "This script only builds the 3.0.17 QA candidate." }
[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
if ([string]$props.Project.PropertyGroup.Version -ne $Version) { throw "Version mismatch with Directory.Build.props" }
if (-not (Test-Path $seed)) { throw "Missing QA baseline content: $seed" }
if (-not (Test-Path $fontPak)) { throw "Missing verified Lexend 650 V12 PAK: $fontPak" }
$essential = @(
  "content\WuWaVH_EN_99_P.pak", "content\WuWaVH_HanViet_99_P.pak",
  "content\loader\version.dll", "content\loader\verorg.dll", "content\loader\WuWaVH.dll",
  "tools\repak.exe"
)
foreach($rel in $essential) { if(-not (Test-Path (Join-Path $seed $rel))) {throw "Missing seed asset: $rel"} }
if (Test-Path $workspace) { Remove-Item $workspace -Recurse -Force }
New-Item -ItemType Directory -Force $stage, $updater | Out-Null
foreach($folder in @("content","Config","editor","Guides","tools")) {
  $from = Join-Path $seed $folder
  if (Test-Path $from) { Copy-Item $from (Join-Path $stage $folder) -Recurse -Force }
}
Write-Output "[1] App publish $Version"
dotnet publish (Join-Path $root "src\VHWuWa.App\VHWuWa.App.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:Version=$Version -o $stage -v quiet
if ($LASTEXITCODE -ne 0) {throw "App publish failed"}
Write-Output "[2] Updater publish $Version"
dotnet publish (Join-Path $root "src\VHWuWa.Updater\VHWuWa.Updater.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:Version=$Version -o $updater -v quiet
if ($LASTEXITCODE -ne 0) {throw "Updater publish failed"}
Copy-Item (Join-Path $updater "VHWuWa.Updater.exe") (Join-Path $stage "VHWuWa.Updater.exe") -Force
$fontDst = Join-Path $stage "content\font"
if (Test-Path $fontDst) {
  Get-ChildItem $fontDst -File -Filter "*.pak" | Remove-Item -Force
}
else { New-Item -ItemType Directory -Force $fontDst | Out-Null }
Copy-Item -LiteralPath $fontPak -Destination (Join-Path $fontDst "WahuFont_100_P.pak") -Force
$filesRequired = @("VHWuWa.exe","VHWuWa.Updater.exe",
  "tools\repak.exe","tools\font-weight-instance.exe","Fonts\fonts.json",
  "Fonts\Lexend-Variable.ttf","Fonts\OFL-Lexend.txt",
  "content\font\WahuFont_100_P.pak") + $essential
foreach($rel in $filesRequired) {
  if(-not(Test-Path (Join-Path $stage $rel))) {throw "Missing package file: $rel"}
}
$catalog = Get-Content (Join-Path $stage "Fonts\fonts.json") -Raw -Encoding UTF8 | ConvertFrom-Json
if ($catalog.count -ne 73 -or $catalog.fonts[0].name -ne "Lexend Variable") {throw "73-font catalog/first choice incorrect"}
$actual = (Get-FileHash (Join-Path $fontDst "WahuFont_100_P.pak") -Algorithm SHA256).Hash
$expected = (Get-FileHash $fontPak -Algorithm SHA256).Hash
if ($actual -ne $expected) {throw "Default Lexend 650 font checksum mismatch"}
foreach($exe in @("VHWuWa.exe","VHWuWa.Updater.exe")) {
  $fv = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $stage $exe)).FileVersion
  if (-not $fv.StartsWith("$Version.")) { throw "EXE version mismatch: $exe = $fv" }
  Write-Output "$exe version=$fv"
}
@"
VHWuWa v3.0.17 - QA RELEASE CANDIDATE
This is a local candidate, NOT an official public release.
PC text EN/HanViet assets are carried over unchanged from 3.0.16 QA baseline.
Default bundled font is genuine Lexend weight 650 in WahuFont_100_P.pak.
Run VHWuWa.exe after extracting the whole ZIP.
Turn off Wuthering Waves/launcher before installing. To preserve external mods,
use 'Tuy chon khac' > 'Cach ly mod cu' BEFORE installing.
QA still required on a real game client, DX11/DX12 and updater rollback.
Do not upload/send release via BotDis until these checks are passed.
"@ | Set-Content -LiteralPath (Join-Path $stage "README_QA_3.0.17.txt") -Encoding UTF8
Write-Output "[3] Create ZIP"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage,$zipPath,[System.IO.Compression.CompressionLevel]::Optimal,$false)
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
  $names = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
  foreach($rel in $filesRequired) {
    $n = $rel.Replace("\","/")
    if($names -notcontains $n){throw "ZIP missing file: $n"}
  }
} finally { $zip.Dispose() }
$sha = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$report = [ordered]@{
  version=$Version;status="RC1_QA_NOT_PUBLIC";zip=$zipPath;zip_sha256=$sha;
  fileCount=$names.Count;default_font="Lexend Variable 650";default_font_pak_sha256=$expected.ToLowerInvariant();
  en_hanviet_content="UNCHANGED_BASELINE_3_0_16_QA";runtime_game_verified=$false;
  updater_e2e_verified=$false;bot_sent=$false;github_published=$false
}
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root "dist\VietHoa-WuWa-v$Version-RC1-QA.json") -Encoding UTF8
Write-Output ("RC_CREATED "+($report|ConvertTo-Json -Compress))
