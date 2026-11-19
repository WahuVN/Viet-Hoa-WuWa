#Requires -Version 5.1
# Read-only regression for the PC mod scanner. Uses only disposable fake game paths.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scan=Join-Path $repo 'scripts\CHECK_WUWA_MOD_CONFLICT.ps1'
if(!(Test-Path $scan)){throw 'SCANNER_MISSING'}
$game=Join-Path $env:TEMP ('WAHU_STOCK_MOUNT_QA_'+[guid]::NewGuid().ToString('N'))
$report=Join-Path $env:TEMP ('WAHU_STOCK_MOUNT_QA_REPORT_'+[guid]::NewGuid().ToString('N')+'.json')
try {
  $win=Join-Path $game 'Client\Binaries\Win64'
  $paks=Join-Path $game 'Client\Content\Paks'
  $mount=Join-Path $game 'Client\Saved\Resources\3.8.0\Mount'
  New-Item -ItemType Directory -Path $win,$paks,$mount -Force|Out-Null
  [IO.File]::WriteAllText((Join-Path $win 'Client-Win64-Shipping.exe'),'FAKE_QA_EXE')
  $names=@('MountOption_Pack_LY_UHD.txt','MountOption_Pack_Volume.txt','MountLauncher.txt','MountLang_en.txt')
  foreach($name in $names){
    [IO.File]::WriteAllText((Join-Path $mount $name),'STOCK_MOUNT')
  }
  [IO.File]::WriteAllText((Join-Path $mount 'ThirdPartyOnly.txt'),'UNRELATED_MOD_SIDECAR')
  & powershell -NoProfile -ExecutionPolicy Bypass -File $scan -GameRoot $game -OutputJson $report|Out-Null
  if($LASTEXITCODE -ne 0){throw 'SCANNER_EXIT_NONZERO'}
  if(!(Test-Path $report)){throw 'SCANNER_DID_NOT_WRITE_REPORT'}
  $result=Get-Content -LiteralPath $report -Raw -Encoding UTF8|ConvertFrom-Json
  $others=@($result.r29.otherMountFiles)
  if($others.Count -ne 1 -or $others[0] -ne 'ThirdPartyOnly.txt'){
    throw ('STOCK_MOUNT_FALSE_POSITIVE: '+($others -join ','))
  }
  foreach($name in $names){
    $file=Join-Path $mount $name
    if(!(Test-Path -LiteralPath $file -PathType Leaf) -or
       [IO.File]::ReadAllText($file) -ne 'STOCK_MOUNT'){
      throw ('SCANNER_CHANGED_STOCK_FILE: '+$name)
    }
  }
  if([IO.File]::ReadAllText((Join-Path $mount 'ThirdPartyOnly.txt')) -ne 'UNRELATED_MOD_SIDECAR'){
    throw 'SCANNER_ALTERED_FOREIGN_MOD_IN_READONLY_MODE'
  }
  Write-Output 'PASS STOCK_MOUNT_POWERSHELL_SCANNER_READONLY=TRUE'
  Write-Output 'PASS STOCK_MOUNT_FOUR_FILES_PRESERVED=TRUE'
  Write-Output 'PASS THIRD_PARTY_SIDECAR_DETECTED=TRUE'
}
finally{
  if(Test-Path $game){Remove-Item -LiteralPath $game -Recurse -Force -ErrorAction SilentlyContinue}
  if(Test-Path $report){Remove-Item -LiteralPath $report -Force -ErrorAction SilentlyContinue}
}
