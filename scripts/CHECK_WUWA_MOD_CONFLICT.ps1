#Requires -Version 5.1
<#
WAHU - Read-only audit of overlapping Wuthering Waves translation loaders.
Does not install, move, overwrite or remove any game file.
Example: powershell -NoProfile -ExecutionPolicy Bypass -File .\CHECK_WUWA_MOD_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves\Wuthering Waves Game"
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$GameRoot,
  [string]$OutputJson = ""
)
$ErrorActionPreference = 'Stop'
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
$win64 = Join-Path $GameRoot 'Client\Binaries\Win64'
$paks = Join-Path $GameRoot 'Client\Content\Paks'
if (-not (Test-Path -LiteralPath $win64 -PathType Container) -or
    -not (Test-Path -LiteralPath $paks -PathType Container)) {
  throw "Duong dan khong dung: can chon folder goc cua game, co Client\Binaries\Win64 va Client\Content\Paks."
}
$loaderNames = @(
  'version.dll','verorg.dll','WuWaVH.dll','winhttp.dll','winmm.dll',
  'wuwaVietHoa.dll','wuwaVietHoa_SDK.dll','wuwaVietHoa_VH.dll',
  'dxgi.dll','dinput8.dll','d3d11.dll','winmm.dll','winhttp.dll'
) | Select-Object -Unique
$waSha = @{
  'version.dll' = 'b328db7a57f3fba676b807112432d462ef7fe8c1b49e1572f89a1fbacaec48b3'
  'verorg.dll' = 'cea99f212af557a9613150c5509631403ed0f05f5d9a06ac42039bd59b40e39a'
  'WuWaVH.dll'  = 'ba4bcbea10ef98c268683df3f87bd1e5090cf9245eb886d84c84ac82e25e4e5b'
}
$loaders = @()
foreach ($name in $loaderNames) {
  $p = Join-Path $win64 $name
  if (!(Test-Path -LiteralPath $p -PathType Leaf)) { continue }
  $it = Get-Item -LiteralPath $p
  $hash = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()
  $owner = 'unknown'
  if ($waSha.ContainsKey($name) -and $hash -eq $waSha[$name]) { $owner = 'WAHU_3.0.17' }
  $loaders += [ordered]@{ name=$name; bytes=$it.Length; sha256=$hash; identifiedAs=$owner }
}
$modFolders = @()
foreach($name in @('~WuWaMods','~mods','~MODS')) {
  $dir = Join-Path $paks $name
  if (!(Test-Path -LiteralPath $dir -PathType Container)) {continue}
  if (@($modFolders | Where-Object {$_.name -ieq $name}).Count -gt 0) {continue}
  $files = @(Get-ChildItem -LiteralPath $dir -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object {$_.Extension -in @('.pak','.sig','.dll') } |
    Select-Object -First 120 @{Name='relative';Expression={$_.FullName.Substring($dir.Length+1)}}, Length)
  $modFolders += [ordered]@{name=$name; files=$files; count=$files.Count }
}
$winPaks = @()
foreach ($folderName in @('wuwaVietHoa','wuwaIndonesia')) {
  $winModPath = Join-Path $win64 $folderName
  if (Test-Path -LiteralPath $winModPath -PathType Container) {
    $winPaks += @(Get-ChildItem -LiteralPath $winModPath -Filter '*.pak' -File -ErrorAction SilentlyContinue |
      Select-Object @{n='folder';e={$folderName}},Name,Length)
  }
}
$warnings = [Collections.Generic.List[string]]::new()
$names=@($loaders |ForEach-Object {$_.name})
$hasWahu= $names -contains 'version.dll' -or @($modFolders|Where-Object {$_.name -eq '~WuWaMods'}).Count -gt 0
if ($hasWahu -and ($names -contains 'winhttp.dll')) {
  $warnings.Add('WAHU version.dll/~WuWaMods + winhttp.dll: co 2 loader, nghi xung dot; can nhan dien nguon DLL.')
}
if ($hasWahu -and ($names -contains 'winmm.dll')) {
  $warnings.Add('WAHU version.dll/~WuWaMods + winmm.dll: co 2 loader, nghi xung dot.')
}
if (@($modFolders|Where-Object {$_.name -eq '~mods'}).Count -gt 0 -and $hasWahu) {
  $warnings.Add('PAK trong ~mods va ~WuWaMods: co the co 2 ban dich/font duoc nap.')
}
if ($winPaks.Count -gt 0 -and $hasWahu) {
  $warnings.Add('PAK trong Win64\wuwaVietHoa/wuwaIndonesia va WAHU: co the co 2 ban dich.')
}
if (@($loaders|Where-Object {
  $_.name -eq 'winhttp.dll' -and
  $_.sha256 -eq 'e757622bb86041860cea04eb164d298c21b5ac3565ef7b44c0d6126939f4e083'
}).Count -gt 0) {
  $warnings.Add('winhttp.dll trung SHA cua mot bo Viet hoa khac; co the gay xung dot.')
}
foreach($rec in $loaders) {
  if ($waSha.ContainsKey($rec.name) -and $rec.identifiedAs -ne 'WAHU_3.0.17') {
    $warnings.Add("$($rec.name) khac SHA WAHU 3.0.17. Chua ket luan la loi/thuoc mod nao.")
  }
}
$marker = Join-Path $paks '~WuWaMods\vhwuwa_install.json'
$markerInfo = @{ present = Test-Path -LiteralPath $marker -PathType Leaf; validJson = $false; variant = $null }
if ($markerInfo.present) {
 try {
   $m = Get-Content -LiteralPath $marker -Encoding UTF8 -Raw | ConvertFrom-Json
   $markerInfo.validJson = $true; $markerInfo.variant=$m.variant
   if ($m.managedFiles) {
     foreach($prop in $m.managedFiles.PSObject.Properties) {
       $rel=$prop.Name -replace '/','\'
       if ($rel -like 'mods\*') {$file=Join-Path (Join-Path $paks '~WuWaMods') ($rel.Substring(5))}
       elseif ($rel -like 'win64\*') {$file=Join-Path $win64 ($rel.Substring(6))}
       else { continue }
       if (!(Test-Path -LiteralPath $file -PathType Leaf)) {
         $warnings.Add("Thieu file WAHU theo marker: $rel")
       } elseif ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $prop.Value) {
         $warnings.Add("File WAHU da bi thay doi so voi marker: $rel")
       }
     }
   }
 } catch { $warnings.Add('Marker WAHU ton tai nhung JSON khong hop le.') }
}

# R29 uses a different location from the old Win64 loader and ~WuWaMods.
$r29Root=Join-Path $GameRoot 'Client\Saved\Resources\3.8.0'
$r29Mount=Join-Path $r29Root 'Mount'
$r29Pak=Join-Path $r29Root 'wahuviethoa\WuWaVH_99_P.pak'
$r29Sig=Join-Path $r29Root 'wahuviethoa\WuWaVH_99_P.sig'
$r29Txt=Join-Path $r29Mount 'wahuviethoa.txt'
$r29FontPak=Join-Path $r29Root 'wahufont\WahuFont_100_P.pak'
$r29FontSig=Join-Path $r29Root 'wahufont\WahuFont_100_P.sig'
$r29FontTxt=Join-Path $r29Mount 'wahufont.txt'
$oldPak=Test-Path -LiteralPath (Join-Path $paks '~WuWaMods\WuWaVH_99_P.pak') -PathType Leaf
$oldFont=Test-Path -LiteralPath (Join-Path $paks '~WuWaMods\WahuFont_100_P.pak') -PathType Leaf
$oldLoader=(Test-Path -LiteralPath (Join-Path $win64 'verorg.dll') -PathType Leaf) -or
           (Test-Path -LiteralPath (Join-Path $win64 'WuWaVH.dll') -PathType Leaf)
$r29Files=[ordered]@{
 pak=(Test-Path -LiteralPath $r29Pak -PathType Leaf)
 sig=(Test-Path -LiteralPath $r29Sig -PathType Leaf)
 mount=(Test-Path -LiteralPath $r29Txt -PathType Leaf)
 fontPak=(Test-Path -LiteralPath $r29FontPak -PathType Leaf)
 fontSig=(Test-Path -LiteralPath $r29FontSig -PathType Leaf)
 fontMount=(Test-Path -LiteralPath $r29FontTxt -PathType Leaf)
}
$r29On=$r29Files.pak -and $r29Files.sig -and $r29Files.mount
$r29FontOn=$r29Files.fontPak -and $r29Files.fontSig -and $r29Files.fontMount
if($r29On -and ($oldPak -or $oldLoader)){
 $warnings.Add('R29 + PAK/loader WAHU 3.7 CU DANG CUNG TON TAI: co nguy co xung dot; khong xoa thu file.')
}
if($r29FontOn -and $oldFont){
 $warnings.Add('R29 FONT + WahuFont_100_P.pak CU CUNG TON TAI: co the trung font khi nap game.')
}
if($r29Files.mount -and !$r29On){
 $warnings.Add('R29 MOUNT DUOC KICH HOAT NHUNG THIEU PAK/SIG: bo cai chua day du.')
}
if(($r29Files.pak -or $r29Files.sig) -and !$r29Files.mount){
 $warnings.Add('R29 PAK/SIG CON SOT NHUNG KHONG CO MOUNT: can doi chieu trang thai cai.')
}
if($r29Files.fontMount -and !$r29FontOn){
 $warnings.Add('R29 FONT MOUNT KHONG DU PAK/SIG: can kiem tra.')
}
if(($r29On -or $r29FontOn) -and ($names -contains 'version.dll') -and !$oldPak -and !$oldLoader){
 $warnings.Add('R29 VA version.dll CUNG TON TAI; chua biet chu so huu version.dll, KHONG XOA THU.')
}
$otherMounts=@()
if(Test-Path -LiteralPath $r29Mount -PathType Container){
 $otherMounts=@(Get-ChildItem -LiteralPath $r29Mount -File -Filter '*.txt' |
   Where-Object { $_.Name -notlike 'MountLang_*' -and $_.Name -notin @('wahuviethoa.txt','wahufont.txt') } |
   Select-Object -First 30 -ExpandProperty Name)
}
if($r29On -and $otherMounts.Count -gt 0){
 $warnings.Add('CO MOUNT KHAC CUNG R29: can kiem tra mod khac; chua khang dinh xung dot.')
}
$r29Sha=$null
if($r29Files.pak){
 $r29Sha=(Get-FileHash -LiteralPath $r29Pak -Algorithm SHA256).Hash
 if($r29Sha -ine '38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598'){
  $warnings.Add('PAK DANG CAI KHONG KHOP SHA256 R29 da phat hanh: co the la ban khac hoac da thay doi.')
 }
}
$r29Info=[ordered]@{
  filesComplete=$r29On
  fontFilesComplete=$r29FontOn
  resourceVersion='3.8.11'
  gameResourceFolder='3.8.0'
  files=$r29Files
  pakSha256=$r29Sha
  legacyPakPresent=$oldPak
  legacyFontPresent=$oldFont
  legacyLoaderPresent=$oldLoader
  otherMountFiles=@($otherMounts)
}

$report = [ordered]@{
 version = 'WAHU_WUWA_MOD_CONFLICT_READONLY_3_R29'
 gameRoot = $GameRoot
 mode = 'READ_ONLY_NO_GAME_WRITE'
 loaderFiles = $loaders
 pakFolders = $modFolders
 win64ModPaks = $winPaks
 marker = $markerInfo
 r29 = $r29Info
 warnings = @($warnings)
}
Write-Output ('WINDOWS_LOADER_COUNT '+$loaders.Count)
foreach($warning in $warnings){Write-Output ('WARNING '+$warning)}
if ($warnings.Count -eq 0){Write-Output 'NO_KNOWN_OVERLAP_FOUND (khong dong nghia game chac chan chay)'}
if ($OutputJson -ne '') {
  $outFull=[IO.Path]::GetFullPath($OutputJson)
  if ($outFull.StartsWith($GameRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)) {
    throw 'Tu choi ghi bao cao vao folder game.'
  }
  $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $outFull -Encoding UTF8
  Write-Output ('REPORT '+$outFull)
} else {
  $report | ConvertTo-Json -Depth 8
}
