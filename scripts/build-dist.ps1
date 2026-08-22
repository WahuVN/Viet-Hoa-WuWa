# build-dist.ps1 — Đóng gói VHWuWa thành BỘ CÀI phát cho người khác
# Publish self-contained (không cần cài .NET) + gói sẵn nội dung Việt hóa (pak Hán Việt/EN + font + loader).
#
#   powershell -ExecutionPolicy Bypass -File scripts\build-dist.ps1
#
$ErrorActionPreference = 'Stop'
$root   = Split-Path -Parent $PSScriptRoot            # ...\VHWuWa
$repo   = Split-Path -Parent $root                    # ...\WuwaVH
$wahu   = Join-Path $repo 'wuwavh_tool\Wahu'
$distRoot = Join-Path $root 'dist'
$out    = Join-Path $distRoot 'VHWuWa_BanCai'
$app    = Join-Path $out 'app'
$content= Join-Path $app 'content'

Write-Host "== 1/4  Publish VHWuWa (self-contained, single-file, nen) ==" -ForegroundColor Cyan
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
  -p:SatelliteResourceLanguages=en `
  -o $app | Out-Null

Write-Host "== 2/4  Gói nội dung Việt hóa (pak + font + loader) ==" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $content, (Join-Path $content 'font'), (Join-Path $content 'loader') | Out-Null

function CopyIf($src, $dst, $label) {
  if (Test-Path $src) { Copy-Item $src $dst -Force; Write-Host "   + $label" }
  else { Write-Host "   ! THIEU: $label ($src)" -ForegroundColor Yellow }
}
CopyIf (Join-Path $wahu 'dist\WuWaVH_HanViet_99_P.pak') $content 'pak Han Viet'
CopyIf (Join-Path $wahu 'dist\WuWaVH_EN_99_P.pak')      $content 'pak Tieng Anh'
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

BẢN 2.0.0 — CỐT TRUYỆN 3.6 + KỸ NĂNG 3.4 / 3.5 / 3.6

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
  ⚠️ Discord Windows: https://discord.gg/c9ws4q9U7
  ⚠️ Discord Android: https://discord.gg/3t5NSyJEz
  ⚠️ GitHub:          https://github.com/WahuVN/Viet-Hoa-WuWa
'@
[System.IO.File]::WriteAllText((Join-Path $out 'DOC TRUOC.txt'), $docTruoc, [System.Text.Encoding]::UTF8)

Write-Host "== 4/4  Nen ZIP de gui ==" -ForegroundColor Green
$sz = [math]::Round(((Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum)/1MB,1)
$zip = Join-Path $distRoot 'VHWuWa_BanCai.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $out -DestinationPath $zip -CompressionLevel Optimal
$zsz = [math]::Round((Get-Item $zip).Length/1MB,1)
$releaseZip = Join-Path $distRoot 'VHWuWa-v2.0.0-Windows.zip'
Copy-Item -LiteralPath $zip -Destination $releaseZip -Force
Write-Host "   Bo cai (thu muc): $out  ($sz MB)"
Write-Host "   File gui (ZIP):   $zip  ($zsz MB)"
Write-Host "   File Release:     $releaseZip"
Write-Host "   -> Gui file VHWuWa_BanCai.zip cho nguoi khac; giai nen ra o dia roi chay 'Chay VHWuWa.bat'."
