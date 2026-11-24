#Requires -Version 5.1
<#
WAHU PC - Scan and reversible cleanup. Unknown files are never deleted.
Examples:
 .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game"
 .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Repair -Apply
 .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode DisableFont -Apply
 .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode DisableR29 -Apply
 .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Restore -BackupPath "..." -Apply
#>
[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$GameRoot,
 [ValidateSet('Scan','Repair','DisableFont','DisableR29','Restore')][string]$Mode='Scan',
 [switch]$Apply,
 [string]$BackupPath=''
)
$ErrorActionPreference='Stop'
$GameRoot=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
$exe=Join-Path $GameRoot 'Client\Binaries\Win64\Client-Win64-Shipping.exe'
$paks=Join-Path $GameRoot 'Client\Content\Paks'
if(!(Test-Path -LiteralPath $exe -PathType Leaf) -or !(Test-Path -LiteralPath $paks -PathType Container)){
 throw 'GAME_PATH_INVALID: chon folder game chua Client.'
}
function NoLinks([string]$path){
 $q=[IO.Path]::GetFullPath($path)
 while($q){
  if(Test-Path -LiteralPath $q){
   if(([IO.File]::GetAttributes($q) -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "REPARSE_POINT_BLOCKED: $q"}
  }
  $next=[IO.Path]::GetDirectoryName($q)
  if(!$next -or $next -eq $q){break};$q=$next
 }
}
NoLinks $GameRoot
function NoGame {
 $proc=@(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
  $_.Name -in @('Client-Win64-Shipping.exe','launcher.exe','launcher_main.exe','KRLauncher.exe') -and
  $_.ExecutablePath -and $_.ExecutablePath.StartsWith($GameRoot+'\',[StringComparison]::OrdinalIgnoreCase)
 })
 if($proc.Count){throw ('GAME_RUNNING: '+($proc.Name -join ','))}
}
$old=@(
 'Client\Binaries\Win64\version.dll',
 'Client\Binaries\Win64\verorg.dll',
 'Client\Binaries\Win64\WuWaVH.dll',
 'Client\Content\Paks\~WuWaMods\WuWaVH_99_P.pak',
 'Client\Content\Paks\~WuWaMods\WuWaVH_99_P.sig',
 'Client\Content\Paks\~WuWaMods\WahuFont_100_P.pak',
 'Client\Content\Paks\~WuWaMods\WahuFont_100_P.sig',
 'Client\Content\Paks\~WuWaMods\vhwuwa_install.json'
)
$pak='Client\Saved\Resources\3.8.0\Vietnamese\Vietnamese_99_P.pak'
$sig='Client\Saved\Resources\3.8.0\Vietnamese\Vietnamese_99_P.sig'
$mount='Client\Saved\Resources\3.8.0\Mount\Vietnamese.txt'
$fPak='Client\Saved\Resources\3.8.0\VietnameseFonts\VietnameseFonts_100_P.pak'
$fSig='Client\Saved\Resources\3.8.0\VietnameseFonts\VietnameseFonts_100_P.sig'
$fMount='Client\Saved\Resources\3.8.0\Mount\VietnameseFonts.txt'
$fReceipt='Client\Saved\Resources\3.8.0\VietnameseFonts\receipt.json'
$allowed=@($old)+@($pak,$sig,$mount,$fPak,$fSig,$fMount,$fReceipt)
function T([string]$rel){
 if($allowed -inotcontains $rel){throw "NOT_ALLOWED: $rel"}
 $p=[IO.Path]::GetFullPath((Join-Path $GameRoot $rel))
 if(!$p.StartsWith($GameRoot+'\', [StringComparison]::OrdinalIgnoreCase)){throw 'PATH_ESCAPE'}
 NoLinks $p
 return $p
}
function Has([string]$rel){return (Test-Path -LiteralPath (T $rel) -PathType Leaf)}
function Sha([string]$p){return (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToUpperInvariant()}
function Sha1([string]$p){return (Get-FileHash -LiteralPath $p -Algorithm SHA1).Hash.ToUpperInvariant()}
function MountExpected([string]$name,[string]$priority,[string]$pack,[string]$signature){
 $nl=[string][char]13+[string][char]10
 return '::Mount::'+$nl+$name+','+$priority+','+(Sha1 $pack)+','+(Sha1 $signature)+',,'+$nl+'::Del::'+$nl
}
function VerifiedR29{
 if(!(Has $pak) -or !(Has $sig) -or !(Has $mount)){return $false}
 if((Sha (T $pak)) -ne '38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598'){return $false}
 return [IO.File]::ReadAllText((T $mount)) -ceq (MountExpected 'Vietnamese/Vietnamese_99_P' '99' (T $pak) (T $sig))
}
function VerifiedFont{
 if(!(Has $fPak) -or !(Has $fSig) -or !(Has $fMount) -or !(Has $fReceipt)){return $false}
 try{
  $m=Get-Content -LiteralPath (T $fReceipt) -Encoding UTF8 -Raw|ConvertFrom-Json
  if($m.Schema -ne 1 -or !([string]$m.SourceName).EndsWith('_100_P.pak') -or
     $m.PakSha256 -ine (Sha (T $fPak)) -or $m.SigSha256 -ine (Sha (T $fSig))){return $false}
  return [IO.File]::ReadAllText((T $fMount)) -ceq (MountExpected 'VietnameseFonts/VietnameseFonts_100_P' '100' (T $fPak) (T $fSig))
 }catch{return $false}
}
if($Mode -eq 'Scan'){
 if($Apply){throw 'SCAN_IS_READONLY'}
 & (Join-Path $PSScriptRoot 'CHECK_WUWA_MOD_CONFLICT.ps1') -GameRoot $GameRoot
 if(!$?){throw 'SCAN_FAILED'}
 return
}
$backupRoot=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'WAHU\WuWaConflictBackups'
if([string]::IsNullOrWhiteSpace($backupRoot) -or
   $backupRoot.StartsWith($GameRoot+'\', [StringComparison]::OrdinalIgnoreCase)){throw 'UNSAFE_BACKUP_LOCATION'}
NoLinks $backupRoot
if($Mode -eq 'Restore'){
 if(!$BackupPath){throw 'BACKUP_PATH_REQUIRED'}
 $from=[IO.Path]::GetFullPath($BackupPath).TrimEnd('\')
 if(!$from.StartsWith($backupRoot+'\', [StringComparison]::OrdinalIgnoreCase)){throw 'INVALID_BACKUP_PATH'}
 NoLinks $from
 $file=Join-Path $from 'manifest.json'
 if(!(Test-Path -LiteralPath $file -PathType Leaf)){throw 'MANIFEST_MISSING'}
 $m=Get-Content -LiteralPath $file -Encoding UTF8 -Raw|ConvertFrom-Json
 if($m.schema -ne 1 -or $m.kind -ne 'WAHU_PC_CONFLICT_BACKUP' -or
    $m.gameRoot -ine $GameRoot){throw 'MANIFEST_WRONG_GAME'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($entry in @($m.files)){
  $rel=[string]$entry.relative
  if(!$seen.Add($rel) -or $allowed -inotcontains $rel -or
     [string]$entry.sha256 -notmatch '^[A-Fa-f0-9]{64}$'){throw 'INVALID_MANIFEST_ENTRY'}
  $saved=Join-Path (Join-Path $from 'files') $rel;NoLinks $saved
  if(!(Test-Path -LiteralPath $saved -PathType Leaf) -or (Sha $saved) -ine $entry.sha256){throw "BACKUP_DAMAGED: $rel"}
  $target=T $rel
  if((Test-Path -LiteralPath $target -PathType Leaf) -and
     (Sha $target) -ine $entry.sha256){throw "RESTORE_WOULD_OVERWRITE: $rel"}
  Write-Output "RESTORE_PREVIEW=$rel"
 }
 if(!$Apply){Write-Output 'RESTORE_PREVIEW_ONLY: add -Apply';return}
 NoGame
 foreach($entry in @($m.files)){
  $target=T ([string]$entry.relative)
  if(Test-Path -LiteralPath $target -PathType Leaf){continue}
  $saved=Join-Path (Join-Path $from 'files') ([string]$entry.relative)
  New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target))|Out-Null
  Copy-Item -LiteralPath $saved -Destination $target -ErrorAction Stop
  if((Sha $target) -ine $entry.sha256){throw 'RESTORE_HASH_FAILURE'}
  Write-Output "RESTORED=$($entry.relative)"
 }
 Write-Output "RESTORE_PASS=TRUE BACKUP_RETAINED=$from"
 return
}
$plan=[Collections.Generic.List[object]]::new()
$holds=[Collections.Generic.List[string]]::new()
function Plan([string]$rel){
 if(Has $rel){
  $f=T $rel
  $plan.Add([pscustomobject]@{relative=$rel;sha256=(Sha $f);size=(Get-Item -LiteralPath $f).Length})
 }
}
if($Mode -eq 'Repair'){
 $known=@{
  'Client\Binaries\Win64\version.dll'=@('B328DB7A57F3FBA676B807112432D462EF7FE8C1B49E1572F89A1FBACAEC48B3')
  'Client\Binaries\Win64\verorg.dll'=@('CEA99F212AF557A9613150C5509631403ED0F05F5D9A06AC42039BD59B40E39A')
  'Client\Binaries\Win64\WuWaVH.dll'=@('BA4BCBEA10EF98C268683DF3F87BD1E5090CF9245EB886D84C84AC82E25E4E5B')
  'Client\Content\Paks\~WuWaMods\WuWaVH_99_P.pak'=@(
   '1E647329564FD159C8F94849B84CFE2B2550703E4354093022416C4BE951FA28',
   '2FB7E68A11C0DD8190887751A93F56AEF12FDE5316929A786585EA46290820B3',
   'F2E47D503D2BE2D55D51552481E270B03A143D86652F680535BB51475DE53E42',
   'F31F40F7444C6E893338BBEE0B2AB721E29B536CB8C07E3BA653E5A665206B98')
  'Client\Content\Paks\~WuWaMods\WahuFont_100_P.pak'=@(
   'FFC75BD057E15E47F288F2C1861B1F63B6443916E0CF255A987A055BD7DA3632')
 }
 $markerRel='Client\Content\Paks\~WuWaMods\vhwuwa_install.json'
 $marker=$null;$valid=$false
 if(Has $markerRel){
  try{
   $marker=Get-Content -LiteralPath (T $markerRel) -Encoding UTF8 -Raw|ConvertFrom-Json
   $v=[version]([string]$marker.packageVersion)
   $valid=($marker.schemaVersion -eq 2 -and $v.Major -eq 3 -and $v.Minor -eq 0 -and
    $v.Build -le 18 -and $null -ne $marker.managedFiles)
  }catch{$valid=$false}
 }
 $verifiedPak=$false
 foreach($rel in $old){
  if($rel -eq $markerRel -or !(Has $rel)){continue}
  $hash=Sha (T $rel)
  $ok=($known.ContainsKey($rel) -and $known[$rel] -contains $hash)
  if($valid){
   $keyPrefix=if($rel.StartsWith('Client\Binaries\Win64\')){'win64/'}else{'mods/'}
   $key=$keyPrefix+[IO.Path]::GetFileName($rel)
   foreach($prop in $marker.managedFiles.PSObject.Properties){
    if($prop.Name -ieq $key -and [string]$prop.Value -match '^[A-Fa-f0-9]{64}$' -and
       [string]$prop.Value -ieq $hash){$ok=$true}
   }
  }
  if($rel -like '*WuWaVH_99_P.pak' -and $ok){$verifiedPak=$true}
  if($ok){Plan $rel}else{$holds.Add("UNVERIFIED_OLD_FILE: $rel")}
 }
 if(Has $markerRel){
  if($valid -and $verifiedPak -and $holds.Count -eq 0){Plan $markerRel}
  else{$holds.Add('OLD_MARKER_NOT_VERIFIED')}
 }
 # Proxy loader without any verified translation PAK is too ambiguous to remove.
 if(!$verifiedPak -and @($plan|Where-Object{$_.relative -like '*\Win64\*'}).Count -gt 0){
  $holds.Add('LOADER_WITHOUT_VERIFIED_OLD_TRANSLATION')
 }
}
elseif($Mode -eq 'DisableFont'){
 if(Has $fMount){if(VerifiedFont){Plan $fMount}else{$holds.Add('FONT_NOT_VERIFIED')}}
}
elseif($Mode -eq 'DisableR29'){
 if(Has $mount){if(VerifiedR29){Plan $mount}else{$holds.Add('R29_NOT_VERIFIED')}}
 if(Has $fMount){if(VerifiedFont){Plan $fMount}else{$holds.Add('FONT_NOT_VERIFIED')}}
}
foreach($entry in $plan){Write-Output "PREVIEW=$($entry.relative) BYTES=$($entry.size)"}
foreach($why in $holds){Write-Output "HOLD=$why"}
if($holds.Count){throw 'NO_FILES_CHANGED: unknown files must be reviewed'}
if(!$plan.Count){Write-Output 'NOTHING_TO_REPAIR=TRUE';return}
if(!$Apply){Write-Output 'PREVIEW_ONLY: add -Apply after review';return}
NoGame
# Complete and SHA-verify all backups BEFORE removing any game file.
$sum=(@($plan|Measure-Object -Property size -Sum).Sum)
$drive=([IO.Path]::GetPathRoot($backupRoot)).Substring(0,1)
if((Get-PSDrive -Name $drive).Free -lt ($sum + 2MB)){throw 'BACKUP_DISK_TOO_FULL'}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
NoLinks $backupRoot
$destRoot=Join-Path $backupRoot ((Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $destRoot|Out-Null
foreach($entry in $plan){
 $src=T $entry.relative;$dest=Join-Path (Join-Path $destRoot 'files') $entry.relative
 New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($dest))|Out-Null
 Copy-Item -LiteralPath $src -Destination $dest -ErrorAction Stop
 if((Sha $dest) -ine $entry.sha256){throw "BACKUP_VERIFY_FAILED: $($entry.relative)"}
}
@{kind='WAHU_PC_CONFLICT_BACKUP';schema=1;gameRoot=$GameRoot;mode=$Mode;
 files=@($plan.ToArray())}|ConvertTo-Json -Depth 6|
 Set-Content -LiteralPath (Join-Path $destRoot 'manifest.json') -Encoding UTF8
$removed=[Collections.Generic.List[object]]::new()
try{
 foreach($entry in $plan){
  $src=T $entry.relative
  if((Sha $src) -ine $entry.sha256){throw "FILE_CHANGED_SINCE_PREVIEW: $($entry.relative)"}
  Remove-Item -LiteralPath $src -Force -ErrorAction Stop
  $removed.Add($entry)
  Write-Output "DISABLED=$($entry.relative)"
 }
}catch{
 $reason=$_
 foreach($entry in $removed){
  $target=T $entry.relative
  if(!(Test-Path -LiteralPath $target)){
   $backup=Join-Path (Join-Path $destRoot 'files') $entry.relative
   Copy-Item -LiteralPath $backup -Destination $target -ErrorAction Stop
   if((Sha $target) -ine $entry.sha256){throw 'ROLLBACK_HASH_FAILED'}
  }
 }
 throw "REPAIR_ROLLED_BACK: $reason"
}
Write-Output "BACKUP_PATH=$destRoot"
Write-Output "REPAIR_PASS=TRUE FILES_DISABLED=$($plan.Count)"
