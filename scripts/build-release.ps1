#requires -Version 5
<#
  Build bản phát hành VHWuWa (win-x64) vào thư mục dist/.
  Dùng: powershell -ExecutionPolicy Bypass -File scripts/build-release.ps1 [-Version 2.1.0]
#>
param([string]$Version = "", [switch]$NoFonts)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Remove-Item (Join-Path $root 'update.json'), (Join-Path $root 'checksums.txt'), (Join-Path $root 'SHA256SUMS.txt') `
  -Force -ErrorAction SilentlyContinue
if (-not $Version) {
    [xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
    $Version = [string]$props.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+([-.+][0-9A-Za-z.-]+)?$') { throw "Version không hợp lệ: $Version" }

$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force $dist | Out-Null

Write-Host "== Test ==" -ForegroundColor Cyan
dotnet test "$root/VHWuWa.sln" -c Release

Write-Host "== Publish App + Updater (1 file .exe duy nhất) ==" -ForegroundColor Cyan
$pub = @("-c","Release","-r","win-x64","--self-contained","true",
         "-p:PublishSingleFile=true","-p:IncludeNativeLibrariesForSelfExtract=true",
         "-p:EnableCompressionInSingleFile=true","-p:Version=$Version")
dotnet publish "$root/src/VHWuWa.App/VHWuWa.App.csproj" @pub -o $dist
dotnet publish "$root/src/VHWuWa.Updater/VHWuWa.Updater.csproj" @pub -o $dist
Remove-Item (Join-Path $dist "*.pdb") -Force -ErrorAction SilentlyContinue

# Thư viện font (Fonts/*.pak + fonts.json) — có thể bỏ bằng -NoFonts để ra bản nhẹ
$fontsSrc = Join-Path $root "Fonts"
if (-not $NoFonts -and (Test-Path $fontsSrc)) {
    $fontsDst = Join-Path $dist "Fonts"
    New-Item -ItemType Directory -Force $fontsDst | Out-Null
    Copy-Item (Join-Path $fontsSrc "*") $fontsDst -Recurse -Force
    $fc = (Get-ChildItem $fontsDst -Filter *.pak).Count
    Write-Host "== Kèm $fc font vào dist\Fonts ==" -ForegroundColor Cyan
}

# Tài liệu kèm theo
Copy-Item "$root/LICENSE" (Join-Path $dist "LICENSE.txt") -Force -ErrorAction SilentlyContinue
Copy-Item "$root/README.md" (Join-Path $dist "README.txt") -Force -ErrorAction SilentlyContinue

if (-not (Test-Path (Join-Path $dist "VHWuWa.exe"))) { throw "Thiếu VHWuWa.exe sau publish." }

$zip = Join-Path $root "VietHoa-WuWa-v$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
$updatePayload = Join-Path $root 'dist-update-payload'
if (Test-Path $updatePayload) { Remove-Item $updatePayload -Recurse -Force }
New-Item -ItemType Directory -Force $updatePayload | Out-Null
Copy-Item (Join-Path $dist '*') $updatePayload -Recurse -Force
$payloadUpdater = Join-Path $updatePayload 'VHWuWa.Updater.exe'
if (Test-Path $payloadUpdater) {
  Move-Item $payloadUpdater (Join-Path $updatePayload 'VHWuWa.Updater.next.exe') -Force
}
Compress-Archive -Path (Join-Path $updatePayload '*') -DestinationPath $zip -Force
Remove-Item $updatePayload -Recurse -Force
$sha = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()

Write-Host "XONG. Thư mục: $dist" -ForegroundColor Green
Write-Host "ZIP: $zip"
Write-Host "SHA-256: $sha"
Write-Host "Chỉ cần upload ZIP lên release v$Version; app đọc phiên bản và SHA-256 trực tiếp từ GitHub."
