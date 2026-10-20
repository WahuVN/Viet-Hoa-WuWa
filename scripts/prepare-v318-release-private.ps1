# Internal-only v3.0.18 packaging. NO GitHub, push or Discord actions.
param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$version = '3.0.18'
$fontReport = Join-Path $root 'build\v318-prep-20261020\V318_FONT_FIX_QA.json'
$fontPak = Join-Path $root 'build\v318-prep-20261020\WahuFont_100_P.pak'
$baseline = Join-Path $root 'build\v317-auto-clean-prep\stage'
$build = Join-Path $root 'build\v318-prep-20261020'
$stage = Join-Path $build 'package_stage'
$appOut = Join-Path $build 'app-publish'
$updOut = Join-Path $build 'updater-publish'
$release = Join-Path $root 'dist\release-prep\v3.0.18'
$zip = Join-Path $release ('VietHoa-WuWa-v'+$version+'.zip')
$sevenZip = 'C:\Program Files\7-Zip\7z.exe'

[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
if ([string]$props.Project.PropertyGroup.Version -ne $version) { throw 'Central app version is not 3.0.18' }
if ((git -C $root branch --show-current).Trim() -ne 'prep/v3.0.18-fonts-autoclean-20261020') {
  throw 'Refusing package build outside dedicated local v3.0.18 prep branch'
}
foreach($file in @($fontPak,$fontReport,$sevenZip,
    (Join-Path $baseline 'VHWuWa.Updater.exe'),
    (Join-Path $baseline 'content\WuWaVH_EN_99_P.pak'),
    (Join-Path $baseline 'content\WuWaVH_HanViet_99_P.pak'))) {
  if(-not (Test-Path -LiteralPath $file)){throw ('Missing package prerequisite: '+$file)}
}
if((Test-Path $stage) -or (Test-Path $release)){throw 'Refusing overwrite of an existing staged release'}
$fontQA = Get-Content $fontReport -Raw -Encoding UTF8 | ConvertFrom-Json
if($fontQA.status -ne 'STATIC_ROUNDTRIP_PASS_PC_RUNTIME_NOT_TESTED' -or
   $fontQA.merged_font_pak_sha256 -ne (Get-FileHash $fontPak -Algorithm SHA256).Hash.ToLowerInvariant() -or
   $fontQA.asset_count -ne 10 -or $fontQA.patched_fonts_from_B119 -ne 9 -or
   -not $fontQA.original_PC_Lexend_preserved){throw 'V3.0.18 font pack has no verified provenance'}
if((Get-PSDrive -Name D).Free -lt 3GB){throw 'Not enough free D: space for safe package staging'}

if(-not $SkipTests){
  Write-Output '[1] Full unit + XAML tests'
  dotnet test (Join-Path $root 'VHWuWa.sln') -c Release -p:UseSharedCompilation=false --verbosity quiet
  if($LASTEXITCODE -ne 0){throw 'Full tests failed'}
}
New-Item -ItemType Directory -Force -Path $stage,$appOut,$updOut,$release|Out-Null
Write-Output '[2] Assemble verified baseline contents'
Copy-Item -Path (Join-Path $baseline '*') -Destination $stage -Recurse -Force
foreach($oldNote in @('README_RC3_UI_QA.txt','README_QA_3.0.17.txt','README_COMPAT_DEV.txt','CHECK_WUWA_MOD_CONFLICT.ps1')){
  $file=Join-Path $stage $oldNote
  if(Test-Path $file){Remove-Item -LiteralPath $file -Force}
}
$fontDestination = Join-Path $stage 'content\font\WahuFont_100_P.pak'
Copy-Item -LiteralPath $fontPak -Destination $fontDestination -Force
if((Get-FileHash $fontDestination -Algorithm SHA256).Hash -ne (Get-FileHash $fontPak -Algorithm SHA256).Hash){
  throw 'Staged font differs from verified B119+Lexend pack'
}

Write-Output '[3] Publish self-contained Windows player 3.0.18'
dotnet publish (Join-Path $root 'src\VHWuWa.App\VHWuWa.App.csproj') -c Release -r win-x64 --self-contained true -p:UseSharedCompilation=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:Version=$version -o $appOut --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'App publish failed'}
Copy-Item (Join-Path $appOut 'VHWuWa.exe') (Join-Path $stage 'VHWuWa.exe') -Force

Write-Output '[4] Publish self-contained updater 3.0.18'
dotnet publish (Join-Path $root 'src\VHWuWa.Updater\VHWuWa.Updater.csproj') -c Release -r win-x64 --self-contained true -p:UseSharedCompilation=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:Version=$version -o $updOut --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Updater publish failed'}
Copy-Item (Join-Path $updOut 'VHWuWa.Updater.exe') (Join-Path $stage 'VHWuWa.Updater.exe') -Force
$required = @('VHWuWa.exe','VHWuWa.Updater.exe',
 'content\WuWaVH_EN_99_P.pak','content\WuWaVH_HanViet_99_P.pak',
 'content\font\WahuFont_100_P.pak','content\loader\version.dll',
 'content\loader\verorg.dll','content\loader\WuWaVH.dll',
 'Fonts\fonts.json','Fonts\Lexend-Variable.ttf','tools\repak.exe')
foreach($path in $required){if(!(Test-Path (Join-Path $stage $path))){throw ('Missing staged '+$path)}}
$expectedEng = '44350C79CBF9CB307752682F0E0F3A31AF983CDC1FEA4B24403B7395237DF547'
$expectedHv = 'E41177B5860465DFB40FBE2F3FDB4504BE1A814A3BFD9BFA4A9DA736D0A5E9E2'
if((Get-FileHash (Join-Path $stage 'content\WuWaVH_EN_99_P.pak') -Algorithm SHA256).Hash -ne $expectedEng -or
   (Get-FileHash (Join-Path $stage 'content\WuWaVH_HanViet_99_P.pak') -Algorithm SHA256).Hash -ne $expectedHv){
  throw 'Previously verified translated PAKs changed'
}
foreach($exe in @('VHWuWa.exe','VHWuWa.Updater.exe')){
  $info=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $stage $exe))
  if(-not $info.FileVersion.StartsWith('3.0.18.')){throw ('Invalid '+$exe+' version '+$info.FileVersion)}
}
$catalog = Get-Content (Join-Path $stage 'Fonts\fonts.json') -Raw -Encoding UTF8|ConvertFrom-Json
if($catalog.count -ne 73 -or $catalog.fonts[0].name -ne 'Lexend Variable'){throw 'Font catalog changed'}

Write-Output '[5] Build private full ZIP and validate CRC'
Push-Location $stage
try{
 & $sevenZip a -tzip -mx=5 -mmt=on $zip '.\*'|Select-Object -Last 9|Out-String -Width 170
 if($LASTEXITCODE -ne 0){throw 'ZIP creation failed'}
} finally {Pop-Location}
& $sevenZip t $zip|Select-Object -Last 8|Out-String -Width 170
if($LASTEXITCODE -ne 0){throw 'ZIP CRC failed'}
Copy-Item (Join-Path $stage 'content\WuWaVH_EN_99_P.pak') (Join-Path $release 'WuWaVH_EN_99_P.pak')
Copy-Item (Join-Path $stage 'content\WuWaVH_HanViet_99_P.pak') (Join-Path $release 'WuWaVH_HanViet_99_P.pak')
$record=[ordered]@{
 status='PRIVATE_RC_BUILT_NOT_PUBLISHED'
 version='3.0.18'
 latest_public_remains='v3.0.17'
 branch=(git -C $root branch --show-current)
 commit=(git -C $root rev-parse HEAD)
 zip=$zip
 zip_bytes=(Get-Item $zip).Length
 zip_sha256=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
 app_sha256=(Get-FileHash (Join-Path $stage 'VHWuWa.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
 updater_sha256=(Get-FileHash (Join-Path $stage 'VHWuWa.Updater.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
 font_pak_sha256=(Get-FileHash $fontDestination -Algorithm SHA256).Hash.ToLowerInvariant()
 font_original_Lexend_preserved=$true
 font_B119_other9_static_QA_pass=$true
 original_EN_HV_unmodified=$true
 github_published=$false
 discord_sent=$false
 desktop_game_runtime_verified=$false
 giant_subtitle_runtime_verified=$false
 update_317_to_318_verified=$false
 assets=@('VietHoa-WuWa-v3.0.18.zip','WuWaVH_EN_99_P.pak','WuWaVH_HanViet_99_P.pak')
}
($record|ConvertTo-Json -Depth 5)|Set-Content (Join-Path $release 'RELEASE_PREFLIGHT.json') -Encoding UTF8
Write-Output ('V318_RELEASE_PRIVATE_READY '+($record|ConvertTo-Json -Compress -Depth 5))
