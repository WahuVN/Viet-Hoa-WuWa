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
  $warnings.Add('winhttp.dll trung SHA ban chinh thuc DangDevVH 2.3.0 Wuwa: co the con loader tu bo Viet hoa khac.')
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
$report = [ordered]@{
 version = 'WAHU_WUWA_MOD_CONFLICT_READONLY_2'
 gameRoot = $GameRoot
 mode = 'READ_ONLY_NO_GAME_WRITE'
 loaderFiles = $loaders
 pakFolders = $modFolders
 win64ModPaks = $winPaks
 marker = $markerInfo
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
