#requires -Version 5
param(
  [string]$CurrentVersion = '3.0.9',
  [string]$NextVersion = '3.0.10'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path ([IO.Path]::GetPathRoot($root)) ('VHWuWa_SelfUpdateE2E_' + [guid]::NewGuid().ToString('N'))
$fixtureProject = Join-Path $root 'tests\VHWuWa.UpdateFixture\VHWuWa.UpdateFixture.csproj'
$updaterProject = Join-Path $root 'src\VHWuWa.Updater\VHWuWa.Updater.csproj'

function Publish-SingleFile([string]$Project, [string]$Output, [string]$Version) {
  dotnet publish $Project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none `
    -p:Version=$Version -o $Output --nologo
  if ($LASTEXITCODE -ne 0) { throw "Publish test fixture failed: $Project v$Version" }
}

function New-InstalledTree([string]$Path, [string]$AppExe, [string]$UpdaterExe) {
  New-Item -ItemType Directory -Force (Join-Path $Path 'Config') | Out-Null
  Copy-Item $AppExe (Join-Path $Path 'VHWuWa.exe') -Force
  Copy-Item $UpdaterExe (Join-Path $Path 'VHWuWa.Updater.exe') -Force
  [IO.File]::WriteAllText((Join-Path $Path 'Config\user.json'), 'preserve-me')
  [IO.File]::WriteAllText((Join-Path $Path 'old-only.txt'), 'old')
}

function New-UpdateZip([string]$Path, [string]$AppExe, [string]$UpdaterExe, [switch]$Unhealthy) {
  $payload = Join-Path $testRoot ('payload-' + [guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force $payload | Out-Null
  Copy-Item $AppExe (Join-Path $payload 'VHWuWa.exe') -Force
  Copy-Item $UpdaterExe (Join-Path $payload 'VHWuWa.Updater.exe') -Force
  [IO.File]::WriteAllText((Join-Path $payload 'introduced-by-update.txt'), 'new')
  if ($Unhealthy) { [IO.File]::WriteAllText((Join-Path $payload 'disable-health.flag'), '1') }
  Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $Path -CompressionLevel Optimal
  Remove-Item $payload -Recurse -Force
}

function Assert-FileVersion([string]$Path, [string]$Expected) {
  $actual = [Diagnostics.FileVersionInfo]::GetVersionInfo($Path).FileVersion
  if (-not $actual.StartsWith("$Expected.")) {
    throw "Sai file version: $Path actual=$actual expected=$Expected"
  }
}

function Invoke-Updater(
  [string]$Runner,
  [string]$Zip,
  [string]$Target,
  [string]$Expected,
  [string]$Sha,
  [switch]$NoRelaunch,
  [Diagnostics.Process]$ParentProcess,
  [string]$ReadyFile
) {
  $arguments = @('--zip', $Zip, '--target', $Target, '--expected-version', $Expected, '--sha256', $Sha)
  if ($NoRelaunch) { $arguments += '--no-relaunch' }
  if ($ParentProcess) {
    $arguments += @(
      '--pid', [string]$ParentProcess.Id,
      '--process-start-ticks', [string]$ParentProcess.StartTime.ToUniversalTime().Ticks,
      '--ready-file', $ReadyFile,
      '--cleanup-dir', $testRoot
    )
  }
  # testRoot is deliberately created directly under the drive root, so these
  # arguments contain no spaces and can be joined safely on Windows PowerShell 5.1.
  $startInfo = [Diagnostics.ProcessStartInfo]::new()
  $startInfo.FileName = $Runner
  $startInfo.Arguments = $arguments -join ' '
  $startInfo.UseShellExecute = $false
  $startInfo.CreateNoWindow = $true
  $process = [Diagnostics.Process]::new()
  $process.StartInfo = $startInfo
  if (-not $process.Start()) { throw 'Could not start updater process.' }

  if ($ParentProcess) {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not (Test-Path $ReadyFile) -and -not $process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
      Start-Sleep -Milliseconds 100
    }
    if (-not (Test-Path $ReadyFile)) {
      throw 'Updater did not create ready-file before the old app exited.'
    }
    Stop-Process -Id $ParentProcess.Id -Force -ErrorAction SilentlyContinue
  }

  if (-not $process.WaitForExit(60000)) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    throw 'Updater E2E timed out after 60 seconds.'
  }
  $process.WaitForExit()
  $exitCode = [int]$process.ExitCode
  $process.Dispose()
  return [pscustomobject]@{
    ExitCode = $exitCode
  }
}

try {
  New-Item -ItemType Directory -Force $testRoot | Out-Null
  $oldApp = Join-Path $testRoot 'old-app'
  $newApp = Join-Path $testRoot 'new-app'
  $oldUpdater = Join-Path $testRoot 'old-updater'
  $newUpdater = Join-Path $testRoot 'new-updater'
  Publish-SingleFile $fixtureProject $oldApp $CurrentVersion
  Publish-SingleFile $fixtureProject $newApp $NextVersion
  Publish-SingleFile $updaterProject $oldUpdater $CurrentVersion
  Publish-SingleFile $updaterProject $newUpdater $NextVersion

  $runner = Join-Path $testRoot 'VHWuWa.Updater.run.exe'
  Copy-Item (Join-Path $oldUpdater 'VHWuWa.Updater.exe') $runner -Force

  # Case 1: real handoff and successful startup health check.
  $installedSuccess = Join-Path $testRoot 'installed-success'
  New-InstalledTree $installedSuccess (Join-Path $oldApp 'VHWuWa.exe') (Join-Path $oldUpdater 'VHWuWa.Updater.exe')
  $successZip = Join-Path $testRoot "VietHoa-WuWa-v$NextVersion.zip"
  New-UpdateZip $successZip (Join-Path $newApp 'VHWuWa.exe') (Join-Path $newUpdater 'VHWuWa.Updater.exe')
  $successSha = (Get-FileHash $successZip -Algorithm SHA256).Hash
  $sleeper = Start-Process powershell.exe -ArgumentList '-NoProfile','-Command','Start-Sleep -Seconds 60' -PassThru -WindowStyle Hidden
  $readyFile = Join-Path $testRoot 'updater-ready.txt'
  $success = Invoke-Updater $runner $successZip $installedSuccess $NextVersion $successSha `
    -ParentProcess $sleeper -ReadyFile $readyFile | Select-Object -Last 1
  if ($success.ExitCode -ne 0) { throw "Success case exit=$($success.ExitCode), expected=0" }
  Assert-FileVersion (Join-Path $installedSuccess 'VHWuWa.exe') $NextVersion
  Assert-FileVersion (Join-Path $installedSuccess 'VHWuWa.Updater.exe') $NextVersion
  if ([IO.File]::ReadAllText((Join-Path $installedSuccess 'Config\user.json')) -ne 'preserve-me') {
    throw 'Success case did not preserve the user configuration.'
  }

  # Case 2: new app does not report health; restore the exact old tree.
  $installedRollback = Join-Path $testRoot 'installed-rollback'
  New-InstalledTree $installedRollback (Join-Path $oldApp 'VHWuWa.exe') (Join-Path $oldUpdater 'VHWuWa.Updater.exe')
  $unhealthyZip = Join-Path $testRoot 'unhealthy.zip'
  New-UpdateZip $unhealthyZip (Join-Path $newApp 'VHWuWa.exe') (Join-Path $newUpdater 'VHWuWa.Updater.exe') -Unhealthy
  $unhealthySha = (Get-FileHash $unhealthyZip -Algorithm SHA256).Hash
  $rollback = Invoke-Updater $runner $unhealthyZip $installedRollback $NextVersion $unhealthySha | Select-Object -Last 1
  if ($rollback.ExitCode -ne 4) { throw "Rollback case exit=$($rollback.ExitCode), expected=4" }
  Assert-FileVersion (Join-Path $installedRollback 'VHWuWa.exe') $CurrentVersion
  Assert-FileVersion (Join-Path $installedRollback 'VHWuWa.Updater.exe') $CurrentVersion
  if (Test-Path (Join-Path $installedRollback 'introduced-by-update.txt')) {
    throw 'Rollback left a file introduced by the failed update.'
  }

  # Case 3: bad SHA is rejected before touching the installed app.
  $installedBadHash = Join-Path $testRoot 'installed-bad-hash'
  New-InstalledTree $installedBadHash (Join-Path $oldApp 'VHWuWa.exe') (Join-Path $oldUpdater 'VHWuWa.Updater.exe')
  $badHashValue = -join ('0' * 64)
  $badHash = Invoke-Updater $runner $successZip $installedBadHash $NextVersion $badHashValue -NoRelaunch | Select-Object -Last 1
  if ($badHash.ExitCode -ne 4) { throw "Ca bad-hash exit=$($badHash.ExitCode), expected=4" }
  Assert-FileVersion (Join-Path $installedBadHash 'VHWuWa.exe') $CurrentVersion

  Write-Host "PASS self-update E2E: $CurrentVersion -> $NextVersion" -ForegroundColor Green
  Write-Host '  + ready handshake before closing the old app'
  Write-Host '  + health handshake and new-version commit'
  Write-Host '  + exact rollback when the new app is unhealthy'
  Write-Host '  + bad SHA rejected before installation'
}
finally {
  Start-Sleep -Seconds 3
  if (Test-Path $testRoot) { Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
