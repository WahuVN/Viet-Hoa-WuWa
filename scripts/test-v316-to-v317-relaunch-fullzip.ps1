# Isolated integration test: v3.0.16 updater performs full 3.0.17 ZIP install
# AND gets a real health handshake from newly launched 3.0.17 GUI.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$old=Join-Path $root 'dist\VietHoa-WuWa-v3.0.16-AUTO-CLEAN-LEXEND-QA-20261013'
$zip=Join-Path $root 'dist\VietHoa-WuWa-v3.0.17-RC1-QA.zip'
$work=Join-Path $root 'build\v317-live-health-audit'
$target=Join-Path $work 'app'
$runner=Join-Path $work 'v316-updater.run.exe'
$cleanup=Join-Path ([IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) ('VHWuWa_Update_QA_317_'+[guid]::NewGuid().ToString('N'))
foreach($x in @($zip,(Join-Path $old 'VHWuWa.exe'),(Join-Path $old 'VHWuWa.Updater.exe'))) {
  if(-not(Test-Path $x)){throw ('Prerequisite missing: '+$x)}
}
if((Get-PSDrive -Name D).Free -lt 4GB){throw 'At least 4 GB free required'}
if(Test-Path $work){Remove-Item $work -Recurse -Force}
New-Item -ItemType Directory -Force $target,$cleanup,(Join-Path $target 'Config')|Out-Null
Copy-Item (Join-Path $old 'VHWuWa.exe') (Join-Path $target 'VHWuWa.exe') -Force
Copy-Item (Join-Path $old 'VHWuWa.Updater.exe') (Join-Path $target 'VHWuWa.Updater.exe') -Force
Copy-Item (Join-Path $old 'VHWuWa.Updater.exe') $runner -Force
[IO.File]::WriteAllText((Join-Path $target 'Config\user.json'),'DONT_LOSE_USER_CONFIG')
$sha=(Get-FileHash $zip -Algorithm SHA256).Hash
$args=@('--zip',$zip,'--target',$target,'--expected-version','3.0.17','--sha256',$sha,'--cleanup-dir',$cleanup)
Write-Output 'TEST_FULL_REAL_APP_START'
$p=Start-Process -FilePath $runner -ArgumentList $args -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $work 'updater.stdout.log') -RedirectStandardError (Join-Path $work 'updater.stderr.log')
if(-not $p.WaitForExit(165000)){ try{$p.Kill()}catch{}; throw 'Updater timeout in isolated full-package GUI test' }
$p.WaitForExit()
$p.Refresh()
$exitCode=$p.ExitCode
Write-Output ('UPDATER_EXIT='+$(if($null -eq $exitCode){'UNAVAILABLE'}else{$exitCode}))
$stdoutPath=Join-Path $work 'updater.stdout.log'
if($null -ne $exitCode -and $exitCode -ne 0){
  if(Test-Path (Join-Path $work 'updater.stderr.log')){Get-Content (Join-Path $work 'updater.stderr.log') -Tail 12}
  throw 'Updater process reported a nonzero exit code'
}
$a=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $target 'VHWuWa.exe')).FileVersion
$u=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $target 'VHWuWa.Updater.exe')).FileVersion
if(-not $a.StartsWith('3.0.17.') -or -not $u.StartsWith('3.0.17.')){throw 'New EXE/updater version mismatch'}
if([IO.File]::ReadAllText((Join-Path $target 'Config\user.json')) -ne 'DONT_LOSE_USER_CONFIG'){throw 'User config lost'}
Write-Output 'PASS_FULL_ZIP_REAL_GUI_HEALTH_HANDSHAKE'
Write-Output ('UPGRADED_APP='+$a+' UPDATER='+$u+' USER_CONFIG_PRESERVED=TRUE')
# Quit ONLY the app that the updater just started in this isolated test folder.
Start-Sleep -Seconds 1
$targetExe=[IO.Path]::GetFullPath((Join-Path $target 'VHWuWa.exe'))
Get-CimInstance Win32_Process -Filter "Name = 'VHWuWa.exe'" -ErrorAction SilentlyContinue |
  Where-Object {$_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath).Equals($targetExe,[StringComparison]::OrdinalIgnoreCase)} |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Milliseconds 900
$successPhrase=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('Q+G6rXAgbmjhuq10IHbDoCBraeG7g20gdHJhIGto4bufaSDEkeG7mW5nIHRow6BuaCBjw7RuZy4='))
$updaterLog=if(Test-Path $stdoutPath){[IO.File]::ReadAllText($stdoutPath,[Text.Encoding]::UTF8)}else{''}
if(-not $updaterLog.Contains($successPhrase)){
  if(Test-Path (Join-Path $work 'updater.stderr.log')){Get-Content (Join-Path $work 'updater.stderr.log') -Tail 8}
  throw 'Updater did not report successful GUI health and commit'
}
Write-Output 'UPDATER_REPORTS_HEALTH_COMMIT=PASS'
Write-Output 'FINAL_E2E_RESULT=PASS'
