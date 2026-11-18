#requires -Version 5.1
param(
 [string]$Version = '3.0.23',
 [string]$OutputRoot = '',
 [string]$R29TemplateStage = 'D:\WAHU_WORK\WUWA_PC_V321_ONECLICK_HOTFIX_RELEASE_WT_20261116\dist\V321_RELEASE_CANDIDATE\PATCH_STAGE',
 [switch]$VerifyOnly,
 [switch]$RequireAuthenticode
)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
if($Version -ne '3.0.23'){throw 'This prep branch is pinned to 3.0.23; review content hashes before changing version'}

if(-not $OutputRoot){
 if($VerifyOnly){$OutputRoot='C:\WAHU_VH_V323_STANDARD_VERIFY'}
 else{$OutputRoot='C:\WAHU_VH_V323_STANDARD_'+(Get-Date -Format yyyyMMdd_HHmmss)}
}
$out=[IO.Path]::GetFullPath($OutputRoot)
$stage=Join-Path $out 'stage'
$app=Join-Path $out 'app'
$up=Join-Path $out 'updater'
$zip=Join-Path $out ("VietHoa-WuWa-v"+$Version+".zip")

$pakSha='38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598'
$videoSha='022703c38d9d0d01e1106c959b7afc8c87bcdd6401aead77bb2e2d48b0fdf8d6'
$fontSha='48a4e2d6cc06cc403a904279b88d9c5275ef304c66d300126cca2489ca7be168'
$fontPakSha='6be60ffc39ce0ee0a31e7dfbda0acc9e243f395ef94a65609baa8146ba62ca68'

function CheckHash([string]$file,[string]$expected){
 if(-not (Test-Path $file -PathType Leaf)){throw "Missing file $file"}
 if((Get-FileHash $file -Algorithm SHA256).Hash -ine $expected){throw "Hash mismatch $file"}
}
function CheckExe([string]$file){
 if(-not (Test-Path $file -PathType Leaf)){throw "Missing executable $file"}
 $v=[Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileVersion
 if($v -ne ($Version+'.0')){throw "Version mismatch $file = $v"}
}
function CheckAuthenticode([string]$file){
 if(-not $RequireAuthenticode){return}
 $sig=Get-AuthenticodeSignature $file
 if($sig.Status -ne 'Valid'){throw "Authenticode required but invalid for ${file}: $($sig.Status)"}
}
function FindSignTool(){
 $kit='C:\Program Files (x86)\Windows Kits\10\bin'
 $tool=Get-ChildItem $kit -Recurse -File -Filter signtool.exe -ErrorAction SilentlyContinue |
   Where-Object {$_.FullName -match '\\x64\\signtool\.exe$'} |
   Sort-Object FullName -Descending |
   Select-Object -First 1
 if(-not $tool){throw 'signtool.exe not found'}
 return $tool.FullName
}
function GetCodeSigningCertificate(){
 $thumb=$env:WAHU_WINDOWS_SIGN_CERT_THUMBPRINT
 if([string]::IsNullOrWhiteSpace($thumb)){throw 'WAHU_WINDOWS_SIGN_CERT_THUMBPRINT is required for production signing'}
 $cert=Get-ChildItem Cert:\CurrentUser\My |
   Where-Object {
     $_.Thumbprint -eq $thumb -and
     $_.HasPrivateKey -and
     $_.NotAfter -gt (Get-Date) -and
     @($_.EnhancedKeyUsageList | Where-Object {$_.ObjectId.Value -eq '1.3.6.1.5.5.7.3.3'}).Count -gt 0
   } |
   Select-Object -First 1
 if(-not $cert){throw 'Configured certificate is missing, expired, lacks a private key, or is not Code Signing'}
 return $cert
}
function SignIfRequired([string]$file){
 if(-not $RequireAuthenticode){return}
 $cert=GetCodeSigningCertificate
 $signtool=FindSignTool
 & $signtool sign /sha1 $cert.Thumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $file
 if($LASTEXITCODE -ne 0){throw "signtool failed for $file"}
 CheckAuthenticode $file
}
function CheckStage(){
 CheckExe (Join-Path $stage 'VHWuWa.exe')
 CheckExe (Join-Path $stage 'VHWuWa.Updater.exe')
 CheckAuthenticode (Join-Path $stage 'VHWuWa.exe')
 CheckAuthenticode (Join-Path $stage 'VHWuWa.Updater.exe')
 CheckHash (Join-Path $stage 'content\3.8.11\WuWaVH_PC_R29_99_P.pak') $pakSha
 CheckHash (Join-Path $stage 'Assets\bg-video.mp4') $videoSha
 CheckHash (Join-Path $stage 'Fonts\Lexend-660.ttf') $fontSha
 CheckHash (Join-Path $stage 'content\font\Lexend-660_100_P.pak') $fontPakSha
 foreach($relative in @(
   'editor\WahuPakEditor.exe',
   'Config\game.json',
   'tools\repak.exe',
   'tools\font-weight-instance.exe',
   'Fonts\fonts.json',
   'content\r29-release.json'
 )){
  if(-not (Test-Path (Join-Path $stage $relative) -PathType Leaf)){throw "Missing $relative"}
 }
 $j=Get-Content (Join-Path $stage 'content\r29-release.json') -Raw | ConvertFrom-Json
 if($j.appVersion -ne $Version -or $j.translationRelease -ne 'R29'){throw 'Invalid release marker'}
 $fonts=Get-Content (Join-Path $stage 'Fonts\fonts.json') -Raw | ConvertFrom-Json
 if($fonts.count -ne 73 -or @($fonts.fonts).Count -ne 73){throw 'Font catalog mismatch'}
}

if($RequireAuthenticode){
 $null=GetCodeSigningCertificate
 $null=FindSignTool
}

if($VerifyOnly){
 CheckStage
 if(-not (Test-Path $zip -PathType Leaf)){throw 'Missing candidate ZIP'}
 & python (Join-Path $repo 'scripts\verify-standard-package.py') --stage $stage --zip $zip --version $Version
 if($LASTEXITCODE -ne 0){throw 'ZIP byte verification failed'}
 'VERIFY_ONLY=PASS'
 exit 0
}

if((Test-Path $stage) -or (Test-Path $app) -or (Test-Path $up) -or (Test-Path $zip)){
 throw "Output exists; never overwrite prior release candidate $out"
}

CheckHash (Join-Path $R29TemplateStage 'content\3.8.11\WuWaVH_PC_R29_99_P.pak') $pakSha
CheckHash (Join-Path $repo 'Assets\bg-video.mp4') $videoSha
CheckHash (Join-Path $repo 'Fonts\Lexend-660.ttf') $fontSha
CheckHash (Join-Path $repo 'content\font\Lexend-660_100_P.pak') $fontPakSha

[xml]$props=Get-Content (Join-Path $repo 'Directory.Build.props')
if($props.Project.PropertyGroup.Version -ne $Version){throw 'Directory.Build.props mismatch'}

New-Item -ItemType Directory -Force $app,$up,$stage | Out-Null
Push-Location $repo
try{
 foreach($proj in @(
   'tests\VHWuWa.Core.Tests\VHWuWa.Core.Tests.csproj',
   'tests\VHWuWa.Infrastructure.Tests\VHWuWa.Infrastructure.Tests.csproj'
 )){
  & dotnet test $proj -c Release -m:1 -v:minimal --nologo
  if($LASTEXITCODE -ne 0){throw "Test failed $proj"}
 }

 & dotnet publish 'src\VHWuWa.App\VHWuWa.App.csproj' -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:SatelliteResourceLanguages=en -p:DebugType=none -p:Version=$Version -o $app --nologo -v:minimal
 if($LASTEXITCODE -ne 0){throw 'App publish failed'}

 & dotnet publish 'src\VHWuWa.Updater\VHWuWa.Updater.csproj' -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:Version=$Version -o $up --nologo -v:minimal
 if($LASTEXITCODE -ne 0){throw 'Updater publish failed'}

 SignIfRequired (Join-Path $app 'VHWuWa.exe')
 SignIfRequired (Join-Path $up 'VHWuWa.Updater.exe')

 & robocopy $R29TemplateStage $stage /E /R:0 /W:0 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -gt 7){throw 'Content overlay failed'}
 & robocopy $app $stage /E /R:0 /W:0 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -gt 7){throw 'App overlay failed'}
 Copy-Item (Join-Path $up 'VHWuWa.Updater.exe') (Join-Path $stage 'VHWuWa.Updater.exe') -Force

 Remove-Item (Join-Path $stage 'Fonts\Lexend-650.ttf') -Force -ErrorAction SilentlyContinue
 Copy-Item (Join-Path $repo 'Fonts\Lexend-660.ttf') (Join-Path $stage 'Fonts\Lexend-660.ttf') -Force
 New-Item -ItemType Directory -Force (Join-Path $stage 'content\font') | Out-Null
 Copy-Item (Join-Path $repo 'content\font\Lexend-660_100_P.pak') (Join-Path $stage 'content\font\Lexend-660_100_P.pak') -Force

 $marker=Join-Path $stage 'content\r29-release.json'
 $j=Get-Content $marker -Raw | ConvertFrom-Json
 if($j.translationRelease -ne 'R29' -or $j.pakSha256 -ne $pakSha){throw 'Template content not R29'}
 $j.appVersion=$Version
 [IO.File]::WriteAllText($marker,($j | ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))

 $notes="WAHU Viet Hoa PC $Version Windows x64: R29, 73-font catalog, Lexend 660, UI Kit, MP4. Install in its own app directory. Existing users can update through the bundled updater with SHA-256 verification and rollback."
 [IO.File]::WriteAllText((Join-Path $stage 'README_VIET_HOA.txt'),$notes,[Text.UTF8Encoding]::new($false))

 CheckStage
 $count=@(Get-ChildItem $stage -File -Recurse).Count
 & python 'scripts\pack-portable-zip.py' $stage $zip --version $Version --expected-files $count
 if($LASTEXITCODE -ne 0){throw 'Canonical package failed'}
 & python 'scripts\verify-standard-package.py' --stage $stage --zip $zip --version $Version
 if($LASTEXITCODE -ne 0){throw 'Byte-for-byte package QA failed'}

 'WIN3_STANDARD_RELEASE_CANDIDATE_BUILD_PASS'
 'OUTPUT='+$zip
} finally {
 Pop-Location
}
