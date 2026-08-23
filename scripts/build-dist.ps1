# build-dist.ps1 — Đóng gói VHWuWa thành BỘ CÀI phát cho người khác
# Publish self-contained (không cần cài .NET) + gói sẵn nội dung Việt hóa (pak Hán Việt/EN + font + loader).
#
#   powershell -ExecutionPolicy Bypass -File scripts\build-dist.ps1 -Version 2.1.0
#
param([string]$Version = "")

$ErrorActionPreference = 'Stop'
$root   = Split-Path -Parent $PSScriptRoot            # ...\VHWuWa
$repo   = Split-Path -Parent $root                    # ...\WuwaVH
$wahu   = Join-Path $repo 'wuwavh_tool\Wahu'
$distRoot = Join-Path $root 'dist'
$buildRoot = Join-Path $distRoot '_build'
$out    = Join-Path $buildRoot 'VHWuWa_BanCai'
$app    = Join-Path $out 'app'
$content= Join-Path $app 'content'
if (-not $Version) {
  [xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
  $Version = [string]$props.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+([-.+][0-9A-Za-z.-]+)?$') { throw "Version không hợp lệ: $Version" }

Write-Host "== 1/4  Publish VHWuWa (self-contained, single-file, nen) ==" -ForegroundColor Cyan
New-Item -ItemType Directory -Force $distRoot, $buildRoot | Out-Null
Remove-Item (Join-Path $distRoot 'update.json'), (Join-Path $distRoot 'checksums.txt'), (Join-Path $distRoot 'SHA256SUMS.txt') `
  -Force -ErrorAction SilentlyContinue
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
# Single-file + nen: gom toan bo runtime .NET vao 1 file VHWuWa.exe (~66 MB thay vi ~140 MB roi rac).
# SatelliteResourceLanguages=en: bo cac ban dich dialog he thong (ja/ko/ru...) khong can thiet.
# KHONG dung PublishTrimmed vi WPF khong ho tro trimming on dinh (de vo app luc chay).
dotnet publish (Join-Path $root 'src\VHWuWa.App\VHWuWa.App.csproj') `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=none `
  -p:Version=$Version `
  -p:SatelliteResourceLanguages=en `
  -o $app

if (-not (Test-Path (Join-Path $app 'VHWuWa.exe'))) {
  throw "Loi nghiem trong: dotnet publish khong tao duoc file VHWuWa.exe!"
}
Write-Host "   + Da build thanh cong VHWuWa.exe ($([math]::Round((Get-Item (Join-Path $app 'VHWuWa.exe')).Length/1MB, 1)) MB)"

# Publish VHWuWa.Updater (Trình tự động cập nhật đè bản mới và mở lại app)
dotnet publish (Join-Path $root 'src\VHWuWa.Updater\VHWuWa.Updater.csproj') `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=none `
  -p:Version=$Version `
  -p:SatelliteResourceLanguages=en `
  -o $app | Out-Null
if (Test-Path (Join-Path $app 'VHWuWa.Updater.exe')) {
  Write-Host "   + Da build thanh cong VHWuWa.Updater.exe ($([math]::Round((Get-Item (Join-Path $app 'VHWuWa.Updater.exe')).Length/1MB, 1)) MB)"
}

Write-Host "== 2/4  Gói nội dung Việt hóa (pak EN + 10 font + loader) ==" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $content, (Join-Path $content 'font'), (Join-Path $content 'loader') | Out-Null

# Danh mục 72 font (tải on-demand khi người dùng chọn trong App)
$appFonts = Join-Path $app 'Fonts'
New-Item -ItemType Directory -Force -Path $appFonts | Out-Null
Copy-Item (Join-Path $root 'Fonts\fonts.json') $appFonts -Force
Write-Host "   + Đã đính kèm danh mục 72 font (chế độ tải on-demand thông minh)"

# Đính kèm công cụ repak.exe hỗ trợ tự động đóng gói font ngoài (.ttf/.otf)
$appTools = Join-Path $app 'tools'
New-Item -ItemType Directory -Force -Path $appTools | Out-Null
$repakSrc = Join-Path $repo 'wuwavh_tool\Wahu\tools\repak.exe'
if (Test-Path $repakSrc) {
  Copy-Item $repakSrc (Join-Path $appTools 'repak.exe') -Force
  Write-Host "   + Đã đính kèm công cụ repak.exe (hỗ trợ tạo font ngoài tự động)"
}

function CopyIf($src, $dst, $label) {
  if (Test-Path $src) { Copy-Item $src $dst -Force; Write-Host "   + $label" }
  else { Write-Host "   ! THIEU: $label ($src)" -ForegroundColor Yellow }
}
# Đóng gói sẵn bản chuẩn Tiếng Anh (Bản Hán Việt và 72 Font được App tự động tải online khi chọn)
CopyIf (Join-Path $wahu 'dist\WuWaVH_EN_99_P.pak') $content 'pak Tieng Anh (Co san)'

# Loader + font: lấy từ bộ cài chuẩn của Wahu (_files), fallback sang Wahu\loader / data
$files = Join-Path $wahu 'dist\WuwaVH_BanCai\_files'
$loaderSrc = if (Test-Path $files) { $files } else { Join-Path $wahu 'loader' }
foreach ($dll in 'version.dll','verorg.dll','WuWaVH.dll') {
  CopyIf (Join-Path $loaderSrc $dll) (Join-Path $content 'loader') "loader\$dll"
}
$fontCandidates = @(
  (Join-Path $files 'WahuFont_100_P.pak'),
  (Join-Path $wahu 'dist\WahuFont_100_P.pak'),
  (Join-Path $wahu 'data\font_BeaufortforLOL-Bold_100_P.pak')
)
$font = $fontCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($font) { CopyIf $font (Join-Path $content 'font') ('font (' + (Split-Path $font -Leaf) + ')') }
else { Write-Host '   ! THIEU: font mac dinh' -ForegroundColor Yellow }

Write-Host "== 3/4  Tao launcher + huong dan ==" -ForegroundColor Cyan
@'
@echo off
chcp 65001 >nul
cd /d "%~dp0app"
start "" "VHWuWa.exe"
'@ | Set-Content -Path (Join-Path $out 'Chay VHWuWa.bat') -Encoding Ascii

$docTruoc = @'
VHWuWa — Bộ cài Việt Hóa Wuthering Waves
========================================

BẢN __VERSION__ — CỐT TRUYỆN 3.6 + KỸ NĂNG 3.4 / 3.5 / 3.6

CÁCH DÙNG
  1. Giải nén cả thư mục "VHWuWa_BanCai" ra ổ đĩa (đừng mở trực tiếp trong file .zip).
  2. Bấm đúp "Chay VHWuWa.bat" (hoặc app\VHWuWa.exe).
  3. Trang chủ -> Chọn thư mục game (tự dò hoặc chọn thủ công).
  4. Cài Việt hóa -> Chọn kiểu tên nhân vật (Hán Việt hoặc Tiếng Anh) -> Bấm Cài đặt.
  5. Vào game: Cài đặt ngôn ngữ hiển thị (Text Language) = English.

MẪU ĐƯỜNG DẪN GAME ĐÚNG
  Chọn đúng:   D:\Game\Wuthering Waves Game  (hoặc E:\Games\Wuthering Waves Game)
  Phải có:     D:\Game\Wuthering Waves Game\Client
  Bản Steam:   ...\steamapps\common\Wuthering Waves\Wuthering Waves Game

LƯU Ý QUAN TRỌNG
  - Hãy TẮT HẲN GAME và launcher trước khi cài hoặc gỡ Việt Hóa.
  - Hai lựa chọn tên là hai bản riêng: Hán Việt hoặc tên nhân vật Tiếng Anh.
  - Đã tích hợp sẵn Font chữ tiếng Việt hiển thị sắc nét, không lỗi ô vuông.
  - Đầy đủ 85 DB dữ liệu: Cốt truyện 3.6 (1.827 câu), Kỹ năng (3.4/3.5/3.6), UI, Nhiệm vụ.
  - Khi cần gỡ: Mở lại App VHWuWa -> Cài Việt hóa -> Bấm Gỡ Việt hóa (khôi phục game sạch).
  - Không cần cài đặt thêm .NET (ứng dụng đã đóng gói sẵn môi trường chạy).

HỖ TRỢ & BÁO LỖI
  ⚠️ Discord Windows: https://discord.gg/tuRCj47sy
  ⚠️ Discord Android: https://discord.gg/3t5NSyJEz
  ⚠️ GitHub:          https://github.com/WahuVN/Viet-Hoa-WuWa
'@
$docTruoc = $docTruoc.Replace('__VERSION__', $Version)
[System.IO.File]::WriteAllText((Join-Path $out 'DOC TRUOC.txt'), $docTruoc, [System.Text.UTF8Encoding]::new($true))

Write-Host "== 4/4  Nen ZIP de gui ==" -ForegroundColor Green
$sz = [math]::Round(((Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum)/1MB,1)
$zip = Join-Path $buildRoot 'VHWuWa_BanCai.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $out -DestinationPath $zip -CompressionLevel Optimal
$zsz = [math]::Round((Get-Item $zip).Length/1MB,1)
$releaseZip = Join-Path $distRoot "VietHoa-WuWa-v$Version.zip"
# Chỉ giữ một gói cập nhật người chơi trong dist để tránh nhầm bản cũ.
Get-ChildItem -LiteralPath $distRoot -File -Filter 'VietHoa-WuWa-v*.zip' -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -ne $releaseZip } |
  Remove-Item -Force
$updatePayload = Join-Path $distRoot '_update_payload'
if (Test-Path $updatePayload) { Remove-Item $updatePayload -Recurse -Force }
New-Item -ItemType Directory -Force $updatePayload | Out-Null
Copy-Item (Join-Path $app '*') $updatePayload -Recurse -Force
# Updater 2.0.0 chạy ngay trong app nên Windows không cho nó tự ghi đè.
# Đổi tên updater mới trong gói giúp người dùng 2.0.0 cập nhật trực tiếp
# lên bất kỳ bản mới nào; app mới sẽ ưu tiên file .next này.
$payloadUpdater = Join-Path $updatePayload 'VHWuWa.Updater.exe'
if (Test-Path $payloadUpdater) {
  Move-Item $payloadUpdater (Join-Path $updatePayload 'VHWuWa.Updater.next.exe') -Force
}
if (Test-Path $releaseZip) { Remove-Item $releaseZip -Force }
Compress-Archive -Path (Join-Path $updatePayload '*') -DestinationPath $releaseZip -CompressionLevel Optimal
Remove-Item $updatePayload -Recurse -Force
$sha = (Get-FileHash $releaseZip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "   Bo cai (thu muc): $out  ($sz MB)"
Write-Host "   File gui (ZIP):   $zip  ($zsz MB)"
Write-Host "   File Release:     $releaseZip"
Write-Host "   SHA-256:           $sha"
Write-Host "   -> Chỉ upload các ZIP/PAK cần phát hành; app đọc SHA-256 trực tiếp từ GitHub."
$uploadGuide = @"
CAC FILE CAN UPLOAD LEN GITHUB RELEASE v$Version
================================================

BAT BUOC CHO NGUOI CHOI / TU CAP NHAT
  VietHoa-WuWa-v$Version.zip

NEU PHAT HANH KEM APP DICH
  App-Dich-WuWa-v$Version.zip

KHONG UPLOAD
  _build\                  Toan bo thu muc va ZIP build trung gian
  RELEASE_BODY.md         Noi dung de copy vao phan mo ta Release

Neu phat hanh kem PAK rieng, chi upload asset da build dung phien ban.
Khong can update.json/checksums.txt: app doc phien ban va SHA-256 tu GitHub Release API.
"@
[System.IO.File]::WriteAllText((Join-Path $distRoot '00_CAN_UPLOAD_GITHUB.txt'), $uploadGuide, [System.Text.UTF8Encoding]::new($true))
