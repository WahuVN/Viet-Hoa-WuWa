# Test the actual published v3.0.20 updater against the full v3.0.21 portable ZIP.
# Works on an isolated app directory; preserves the user's existing desktop link.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path 'dist/V321_RELEASE_CANDIDATE').Path
$qaBase = 'C:\WAHU_QA_V321_20261116'
$target = Join-Path $qaBase 'E2E_REAL_INSTALLED_320'
$tempRoot = Join-Path $qaBase 'Temp'
if (-not (Test-Path $target) -or -not (Test-Path $tempRoot)) { throw 'QA_SANDBOX_ON_C_MISSING' }
# Use C: for updater preflight, temporary extraction, and startup health.
# These environment variables affect only this short-lived QA process and its children.
$env:TEMP = $tempRoot
$env:TMP = $tempRoot
$runner = Join-Path $root 'E2E_PUBLIC_V320_UPDATER.run.exe'
$zip = Join-Path $root 'release/VietHoa-WuWa-v3.0.21.zip'
$expected = ((Get-Content (Join-Path $root 'release/SHA256SUMS.txt') -TotalCount 1) -split '\s+')[0].ToUpperInvariant()
if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected) { throw 'ZIP_SHA_MISMATCH' }
if ((Get-Item $runner).VersionInfo.FileVersion -ne '3.0.20.0') { throw 'PUBLISHED_UPDATER_NOT_V320' }
if ((Get-Item (Join-Path $target 'VHWuWa.exe')).VersionInfo.FileVersion -ne '3.0.20.0') { throw 'TARGET_NOT_V320' }
$other = @(Get-CimInstance Win32_Process -Filter "name='VHWuWa.exe'")
if ($other.Count -gt 0) { throw 'OTHER_VHWUWA_PROCESS_RUNNING' }
$desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)
$link = Join-Path $desktop 'WuWa.lnk'
$legacyLink = Join-Path $desktop 'VHWuWa.lnk'
$linkExisted = Test-Path $link
$legacyExisted = Test-Path $legacyLink
$linkData = if ($linkExisted) { [IO.File]::ReadAllBytes($link) } else { $null }
$legacyData = if ($legacyExisted) { [IO.File]::ReadAllBytes($legacyLink) } else { $null }
$qaApp = Join-Path $target 'VHWuWa.exe'
$testTemp = Join-Path ([IO.Path]::GetTempPath()) ('VHWuWa_Update_real_e2e_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testTemp -Force | Out-Null
$code = 99
try {
    $psi = [Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $runner
    $psi.Arguments = '--zip ' + $zip + ' --target ' + $target + ' --expected-version 3.0.21 --sha256 ' + $expected + ' --relaunch VHWuWa.exe --cleanup-dir ' + $testTemp
    $psi.WorkingDirectory = $root
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $proc = [Diagnostics.Process]::new()
    $proc.StartInfo = $psi
    if (-not $proc.Start()) { throw 'OLD_PUBLIC_UPDATER_DID_NOT_START' }
    Write-Output ('REAL_E2E_UPDATER_PID=' + $proc.Id)
    if (-not $proc.WaitForExit(200000)) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        throw 'REAL_UPDATER_TIMEOUT'
    }
    $code = [int]$proc.ExitCode
    # A WPF child inherits stdout/stderr pipe handles from the updater. Close only
    # our isolated GUI before reading the pipe; otherwise ReadToEnd can block.
    $qaAppsBeforeRead = @(Get-CimInstance Win32_Process -Filter "name='VHWuWa.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.ExecutablePath -eq $qaApp })
    foreach ($qa in $qaAppsBeforeRead) {
        Stop-Process -Id $qa.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Output ('CLOSED_QA_GUI_AFTER_UPDATER_EXIT=' + $qa.ProcessId)
    }
    $proc.WaitForExit()
    $out = $proc.StandardOutput.ReadToEnd()
    $err = $proc.StandardError.ReadToEnd()
    $proc.Dispose()
    Write-Output ('REAL_UPDATER_EXIT=' + $code)
    Write-Output ('REAL_UPDATER_STDOUT=' + $out)
    if ($err) { Write-Output ('REAL_UPDATER_STDERR=' + $err) }
    if ($code -ne 0) { throw 'REAL_UPDATER_RETURNED_FAILURE' }
    $app = Join-Path $target 'VHWuWa.exe'
    $updater = Join-Path $target 'VHWuWa.Updater.exe'
    if ((Get-Item $app).VersionInfo.FileVersion -ne '3.0.21.0') { throw 'APP_VERSION_NOT_321' }
    if ((Get-Item $updater).VersionInfo.FileVersion -ne '3.0.21.0') { throw 'UPDATER_VERSION_NOT_321' }
    $pak = Join-Path $target 'content/3.8.11/WuWaVH_PC_R29_99_P.pak'
    if ((Get-FileHash $pak -Algorithm SHA256).Hash -ne '38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598') { throw 'R29_CHANGED' }
    $marker = Get-Content (Join-Path $target 'content/r29-release.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($marker.appVersion -ne '3.0.21') { throw 'MARKER_NOT_V321' }
    Write-Output ('UPDATED_APP_SHA=' + (Get-FileHash $app -Algorithm SHA256).Hash)
    Write-Output ('UPDATED_UPDATER_SHA=' + (Get-FileHash $updater -Algorithm SHA256).Hash)
    Write-Output 'REAL_PUBLIC_V320_TO_V321_WPF_E2E=PASS'
}
finally {
    $qaApps = @(Get-CimInstance Win32_Process -Filter "name='VHWuWa.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.ExecutablePath -eq $qaApp })
    foreach ($qa in $qaApps) {
        Stop-Process -Id $qa.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Output ('CLOSED_ONLY_QA_APP_PID=' + $qa.ProcessId)
    }
    if ($linkExisted) {
        if (-not (Test-Path $link) -or -not (([Convert]::ToBase64String([IO.File]::ReadAllBytes($link)) -eq [Convert]::ToBase64String($linkData)))) {
            [IO.File]::WriteAllBytes($link, $linkData)
            Write-Output 'DESKTOP_LINK_RESTORED=YES'
        }
    }
    elseif (Test-Path $link) {
        Remove-Item $link -Force
        Write-Output 'QA_CREATED_DESKTOP_LINK_REMOVED=YES'
    }
    if ($legacyExisted) {
        if (-not (Test-Path $legacyLink) -or -not (([Convert]::ToBase64String([IO.File]::ReadAllBytes($legacyLink)) -eq [Convert]::ToBase64String($legacyData)))) {
            [IO.File]::WriteAllBytes($legacyLink, $legacyData)
            Write-Output 'LEGACY_LINK_RESTORED=YES'
        }
    }
    if (Test-Path $testTemp) {
        Remove-Item $testTemp -Force -Recurse -ErrorAction SilentlyContinue
    }
}
