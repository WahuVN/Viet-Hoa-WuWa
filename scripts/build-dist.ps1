# build-dist.ps1 — Đóng gói VHWuWa thành BỘ CÀI phát cho người khác
# Publish self-contained (không cần cài .NET) + gói sẵn nội dung Việt hóa (pak Hán Việt/EN + font + loader).
#
#   powershell -ExecutionPolicy Bypass -File scripts\build-dist.ps1 -Version 3.0.6
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
Write-Host "== 0/5  Kiem tra chat luong ma nguon (dotnet test & XAML validation) ==" -ForegroundColor Magenta
dotnet test (Join-Path $root 'VHWuWa.sln') -c Release --nologo
if ($LASTEXITCODE -ne 0) {
  throw "Loi nghiem trong: Unit Tests hoac XAML Resource Validation that bai! Build bi huy bo lap tuc."
}
Write-Host "   + Tat ca bai kiem thu deu DAT (100% Passed)" -ForegroundColor Green

Write-Host "== 1/5  Publish VHWuWa (self-contained, single-file, nen) ==" -ForegroundColor Cyan
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

# Không chép runtime .NET lần hai cho updater (~33,5 MB). App dùng trình cập
# nhật PowerShell có backup/rollback; PowerShell đã có sẵn trên Windows 10/11.
Write-Host '   + Trinh cap nhat gon dung PowerShell cua Windows (khong lap runtime .NET)'

Write-Host "== 2/5  Gói nội dung Việt hóa (PAK VI + font + loader) ==" -ForegroundColor Cyan
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
# Chỉ đóng gói bản VI/tên quốc tế mặc định. Bản Hán Việt được tải từ asset
# GitHub khi người dùng chọn, hoặc được công cụ chỉnh sửa tự dựng sau khi lưu.
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

Write-Host "== 3/5  Đóng gói bộ Đặt tên nhân vật / Thuật ngữ ==" -ForegroundColor Cyan
$editor = Join-Path $app 'editor'
& (Join-Path $PSScriptRoot 'prepare-pak-editor.ps1') -Target $editor
if (-not $?) { throw 'Đóng gói Pak Editor thất bại.' }

Write-Host "== 4/5  Tao launcher + huong dan ==" -ForegroundColor Cyan
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
  - Đầy đủ 86 DB dữ liệu: Cốt truyện 3.6 (1.827 câu), Kỹ năng (3.4/3.5/3.6), UI, Nhiệm vụ.
  - Khi cần gỡ: Mở lại App VHWuWa -> Cài Việt hóa -> Bấm Gỡ Việt hóa (khôi phục game sạch).
  - Không cần cài đặt thêm .NET (ứng dụng đã đóng gói sẵn môi trường chạy).

HỖ TRỢ & BÁO LỖI
  ⚠️ Discord Windows: https://discord.gg/tuRCj47sy
  ⚠️ Discord Android: https://discord.gg/3t5NSyJEz
  ⚠️ GitHub:          https://github.com/WahuVN/Viet-Hoa-WuWa
'@
$docTruoc = $docTruoc.Replace('__VERSION__', $Version)
[System.IO.File]::WriteAllText((Join-Path $out 'DOC TRUOC.txt'), $docTruoc, [System.Text.UTF8Encoding]::new($true))

Write-Host "== 5/5  Nen ZIP de gui ==" -ForegroundColor Green
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
  WuWaVH_EN_99_P.pak         Nut khoi phuc mac dinh tai truc tiep asset nay
  WuWaVH_HanViet_99_P.pak    Nut khoi phuc mac dinh tai truc tiep asset nay

NEU PHAT HANH KEM APP DICH
  App-Dich-WuWa-v$Version.zip

KHONG UPLOAD
  _build\                  Toan bo thu muc va ZIP build trung gian
  RELEASE_BODY.md         Noi dung de copy vao phan mo ta Release

Copy hai PAK tu wuwavh_tool\Wahu\dist vao release.
Khong can update.json/checksums.txt: app doc phien ban va SHA-256 tu GitHub Release API.
"@
[System.IO.File]::WriteAllText((Join-Path $distRoot '00_CAN_UPLOAD_GITHUB.txt'), $uploadGuide, [System.Text.UTF8Encoding]::new($true))
$releaseNotes = Join-Path $root "RELEASE_NOTES_v$Version.md"
$releaseBody = Join-Path $distRoot 'RELEASE_BODY.md'
if (Test-Path -LiteralPath $releaseNotes) {
  Copy-Item -LiteralPath $releaseNotes -Destination $releaseBody -Force
  Write-Host "   Release body:      $releaseBody"
}
elseif (Test-Path -LiteralPath $releaseBody) {
  Remove-Item -LiteralPath $releaseBody -Force
  Write-Host "   ! Chưa có RELEASE_NOTES_v$Version.md; không giữ release body cũ" -ForegroundColor Yellow
}
