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

@'
VHWuWa — Bộ cài Việt Hóa Wuthering Waves
========================================

BAN 2.0.0 — COT TRUYEN 3.6 + KY NANG 3.4/3.5/3.6

CÁCH DÙNG
  1. Giải nén cả thư mục "VHWuWa_BanCai" ra ổ đĩa (đừng để trong .zip).
  2. Bấm đúp "Chay VHWuWa.bat" (hoặc app\VHWuWa.exe).
  3. Trang chủ  -> Tự dò / Chọn thư mục game (hỗ trợ bản Launcher & Steam).
  4. Cài Việt hóa -> chọn kiểu tên nhân vật (Hán Việt / Tiếng Anh) -> Cài.
  5. Vào game: Text Language = English, chơi bằng DirectX 11.

MẪU ĐƯỜNG DẪN GAME ĐÚNG
  Kuro:  E:\Games\Wuthering Waves\Wuthering Waves Game
  Steam: D:\SteamLibrary\steamapps\common\Wuthering Waves

  Thư mục được chọn phải có thư mục "Client" ngay bên trong.
  Hãy chọn "...\Wuthering Waves Game", KHÔNG chọn Client, Saved hoặc Paks.

LƯU Ý
  - TẮT HẲN GAME và launcher trước khi cài/gỡ; app sẽ chặn nếu vẫn còn chạy.
  - App tự kiểm tra PAK/mod/loader khác và không cài đè để tránh xung đột.
  - Hai lựa chọn tên là hai PAK riêng: Hán Việt hoặc tên tiếng Anh.
  - Gồm 85 DB dịch hữu dụng đã kiểm chứng: thoại, UI, thuộc tính, kỹ năng, vật phẩm và nhiệm vụ.
  - Cốt truyện chính 3.6: 1.827/1.827 câu VI/HV.
  - Kỹ năng: 3.4 = 1.766/1.766; 3.5 = 93/93; 3.6 = 97/97.
  - Không đóng các DB placeholder/rỗng hoặc DB hệ thống không có nội dung cần dịch.
  - Dòng UID/Mã đặc trưng được để trống hoàn toàn; font Việt mặc định được cài cùng bản dịch.
  - Mod runtime có thể bị anti-cheat đánh dấu -> nên dùng TÀI KHOẢN PHỤ.
  - Gỡ: mở app -> Cài Việt hóa -> Gỡ Việt hóa (khôi phục file gốc).
  - Không cần cài .NET (đã đóng gói sẵn).
'@ | Set-Content -Path (Join-Path $out 'DOC TRUOC.txt') -Encoding UTF8

Write-Host "== 4/4  Nen ZIP de gui ==" -ForegroundColor Green
$sz = [math]::Round(((Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum)/1MB,1)
$zip = Join-Path $distRoot 'VHWuWa_BanCai.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $out -DestinationPath $zip -CompressionLevel Optimal
$zsz = [math]::Round((Get-Item $zip).Length/1MB,1)
$zipAlias = Join-Path $distRoot 'VHWuWa_BanC2ai.zip'
Copy-Item -LiteralPath $zip -Destination $zipAlias -Force
Write-Host "   Bo cai (thu muc): $out  ($sz MB)"
Write-Host "   File gui (ZIP):   $zip  ($zsz MB)"
Write-Host "   Ten viet tat dong bo: $zipAlias"
Write-Host "   -> Gui file VHWuWa_BanCai.zip cho nguoi khac; giai nen ra o dia roi chay 'Chay VHWuWa.bat'."
