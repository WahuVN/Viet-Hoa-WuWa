#Requires -Version 5.1
<#
  WAHU - Release preparation gate for PC. Only tests SOURCE in isolated QA.
  It never modifies a real game, deployed binary or GitHub release.
  SourceOnly: unit tests + WPF build + R29 smoke when a verified PAK is supplied.
  Release: adds manual game-runtime evidence and clean Git working tree requirements.

  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\QA_TRUOC_PHAT_HANH_PC.ps1 -R29Pak "D:\QA\WuWaVH_PC_R29_99_P.pak"
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\QA_TRUOC_PHAT_HANH_PC.ps1 -Mode Release -R29Pak "..." -RuntimeEvidence "D:\QA\runtime-evidence.json"
#>
[CmdletBinding()]
param(
  [ValidateSet('SourceOnly','Release')][string]$Mode = 'SourceOnly',
  [string]$R29Pak = '',
  [string]$RuntimeEvidence = ''
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $repo
$report = [ordered]@{
  schema = 'WAHU_PC_RELEASE_GATE_20261119'
  mode = $Mode
  checkedAt = (Get-Date).ToString('o')
  sourceHead = ''
  sourceDirtyCount = 0
  steps = [ordered]@{}
  runtime = 'HOLD_NOT_VALIDATED'
  readyToRelease = $false
}
function RunStep([string]$name,[string]$exe,[string[]]$arguments){
  Write-Output ('BEGIN='+$name)
  & $exe @arguments
  $code = $LASTEXITCODE
  $report.steps[$name] = [ordered]@{exitCode = $code; passed = ($code -eq 0)}
  if($code -ne 0){throw ('GATE_FAILED: '+$name+' exited '+$code)}
  Write-Output ('PASS='+$name)
}
function ExpectedPak([string]$path){
  if([string]::IsNullOrWhiteSpace($path) -or !(Test-Path -LiteralPath $path -PathType Leaf)){
    throw 'R29_PAK_MISSING: Provide an existing, verified PAK file.'
  }
  $expected='38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598'
  if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $expected){
    throw 'R29_PAK_SHA256_MISMATCH: Stop; do not build or publish.'
  }
  return [IO.Path]::GetFullPath($path)
}
try {
  if(!(Test-Path (Join-Path $repo 'VHWuWa.sln'))){throw 'RUN_FROM_WRONG_REPO'}
  $head=(& git -C $repo rev-parse HEAD)
  if($LASTEXITCODE -ne 0){throw 'GIT_HEAD_UNAVAILABLE'}
  $report.sourceHead = $head.Trim()
  $dirty=@(& git -C $repo status --porcelain=v1 --untracked-files=normal)
  if($LASTEXITCODE -ne 0){throw 'GIT_STATUS_UNAVAILABLE'}
  $report.sourceDirtyCount = $dirty.Count
  if($Mode -eq 'Release' -and $dirty.Count){throw 'RELEASE_SOURCE_DIRTY: Commit/review all changes first.'}

  $processes=@(Get-Process -Name 'Client-Win64-Shipping','Wuthering Waves','launcher_main' -ErrorAction SilentlyContinue)
  if($processes.Count -gt 0 -and $Mode -eq 'Release'){
    throw 'GAME_OR_LAUNCHER_RUNNING: Do not prepare final release while game is active.'
  }

  RunStep 'StockMountReadOnlyScan' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File','tests/StockMountPowerShellQa.ps1')
  RunStep 'InfrastructureTests' 'dotnet' @('test','tests/VHWuWa.Infrastructure.Tests/VHWuWa.Infrastructure.Tests.csproj','-c','Release','--nologo','--verbosity','quiet')
  RunStep 'CoreTests' 'dotnet' @('test','tests/VHWuWa.Core.Tests/VHWuWa.Core.Tests.csproj','-c','Release','--nologo','--verbosity','quiet')
  RunStep 'WpfAppBuild' 'dotnet' @('build','src/VHWuWa.App/VHWuWa.App.csproj','-c','Release','--nologo','--verbosity','quiet')

  if($R29Pak){
    $pak=ExpectedPak $R29Pak
    RunStep 'R29RealPakSmoke' 'dotnet' @('run','-c','Release','--project','tests/VHWuWa.R29SmokeQa/VHWuWa.R29SmokeQa.csproj','--',$pak,'--auto-conflicts-only')
  } elseif($Mode -eq 'Release'){
    throw 'RELEASE_HOLD: Real R29 PAK smoke test is mandatory.'
  } else {
    $report.steps['R29RealPakSmoke'] = [ordered]@{exitCode = $null;passed = $false;result='HOLD_MISSING_R29_PAK'}
    throw 'SOURCE_QA_HOLD: Provide a verified R29 PAK to complete source QA.'
  }

  if($Mode -eq 'Release'){
    if(!$RuntimeEvidence -or !(Test-Path -LiteralPath $RuntimeEvidence -PathType Leaf)){
      throw 'RELEASE_HOLD: Missing independent manual test record from a REAL game.'
    }
    $proof=Get-Content -LiteralPath $RuntimeEvidence -Raw -Encoding UTF8 |ConvertFrom-Json
    if(!$proof.gameVersion -or !$proof.appVersion -or !$proof.pakSha256 -or
       $proof.pakSha256 -ine '38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598' -or
       !$proof.cleanLaunchPassed -or !$proof.legacyUpgradePassed -or
       !$proof.thirdPartyModUpgradePassed -or !$proof.uninstallRestoresGameLaunch -or
       [int]$proof.coldLaunchCount -lt 2 -or !$proof.evidenceDirectory){
      throw 'RELEASE_HOLD: Runtime evidence does not cover launch, legacy mods, uninstall and two cold starts.'
    }
    if(!(Test-Path -LiteralPath $proof.evidenceDirectory -PathType Container)){
      throw 'RELEASE_HOLD: Runtime evidence folder does not exist.'
    }
    if(@(Get-ChildItem -LiteralPath $proof.evidenceDirectory -File).Count -eq 0){
      throw 'RELEASE_HOLD: Runtime evidence folder is empty.'
    }
    $report.runtime='MANUAL_EVIDENCE_PRESENT_REVIEW_STILL_REQUIRED'
    $report.readyToRelease=$false
    # Still require HUMAN verification of Steam/Kuro, DX11/DX12 and resource compatibility.
    Write-Output 'MANUAL_REVIEW_REQUIRED: evidence presence is NOT proof of runtime compatibility.'
    throw 'RELEASE_HOLD: Manual Steam/Kuro, DirectX and current game version sign-off required.'
  }
  Write-Output ('SOURCE_QA_PASS=TRUE GATE_MODE='+$Mode)
  Write-Output ('RELEASE_READY='+$report.readyToRelease)
}
catch {
  $report.steps['Failure'] = [ordered]@{passed=$false;detail=$_.Exception.Message}
  Write-Output ('GATE_HOLD_OR_FAIL='+$_.Exception.Message)
  throw
}
finally{
  $dir=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'VHWuWa\QaReports'
  New-Item -Path $dir -ItemType Directory -Force |Out-Null
  $file=Join-Path $dir ('pc-qa-'+(Get-Date -Format yyyyMMdd-HHmmss)+'-'+[guid]::NewGuid().ToString('N').Substring(0,5)+'.json')
  $report |ConvertTo-Json -Depth 8 |Set-Content -LiteralPath $file -Encoding UTF8
  Write-Output ('QA_GATE_REPORT='+$file)
  Pop-Location
}
