# Real v3.0.16 updater + real v3.0.17 RC1 ZIP in a temporary isolated app folder.
# Does not use GitHub, change a user's actual installation or publish any files.
param([string]$RcZip = "")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$old = Join-Path $root 'dist\VietHoa-WuWa-v3.0.16-AUTO-CLEAN-LEXEND-QA-20261013'
if (-not $RcZip) { $RcZip = Join-Path $root 'dist\VietHoa-WuWa-v3.0.17-RC1-QA.zip' }
$zip = [IO.Path]::GetFullPath($RcZip)
$work = Join-Path $root 'build\v317-real-updater-audit'
$target = Join-Path $work 'installed-v316'
$runner = Join-Path $work 'VHWuWa.Updater.run.exe'
$required = @((Join-Path $old 'VHWuWa.exe'),(Join-Path $old 'VHWuWa.Updater.exe'),$zip)
foreach($path in $required){ if(-not(Test-Path $path)){throw 'Missing prerequisite: '+$path} }
$disk=(Get-PSDrive D).Free
if($disk -lt 4GB){throw 'Insufficient D space for upgrade integration test; need at least 4 GB'}
if(Test-Path $work){Remove-Item $work -Recurse -Force}
New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item (Join-Path $old 'VHWuWa.exe') (Join-Path $target 'VHWuWa.exe') -Force
Copy-Item (Join-Path $old 'VHWuWa.Updater.exe') (Join-Path $target 'VHWuWa.Updater.exe') -Force
Copy-Item (Join-Path $old 'VHWuWa.Updater.exe') $runner -Force
New-Item -ItemType Directory -Force (Join-Path $target 'Config') | Out-Null
[IO.File]::WriteAllText((Join-Path $target 'Config\user.json'),'PRESERVE_V316_USER')
[IO.File]::WriteAllText((Join-Path $target 'old-only.txt'),'PRESERVE_OLD_ONLY')
$sha=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
function CheckVersion([string]$path,[string]$expected){
  $actual=[Diagnostics.FileVersionInfo]::GetVersionInfo($path).FileVersion
  if(-not $actual.StartsWith("$expected.")){throw "Version failed $path actual=$actual expected=$expected"}
}
CheckVersion (Join-Path $target 'VHWuWa.exe') '3.0.16'
CheckVersion $runner '3.0.16'
$arguments=@('--zip',$zip,'--target',$target,'--expected-version','3.0.17','--sha256',('0'*64),'--no-relaunch')
Write-Output 'TEST1_INVALID_SHA_START'
$bad = Start-Process -FilePath $runner -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
Write-Output ('TEST1_EXIT='+$bad.ExitCode)
if($bad.ExitCode -ne 4){throw 'Expected wrong-SHA exit 4'}
CheckVersion (Join-Path $target 'VHWuWa.exe') '3.0.16'
if([IO.File]::ReadAllText((Join-Path $target 'Config\user.json')) -ne 'PRESERVE_V316_USER'){throw 'Bad SHA touched user config'}
Write-Output 'TEST1_BAD_SHA_REFUSED_PASS'
Write-Output 'TEST2_REAL_ZIP_START'
$arguments=@('--zip',$zip,'--target',$target,'--expected-version','3.0.17','--sha256',$sha,'--no-relaunch')
$good=Start-Process -FilePath $runner -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
Write-Output ('TEST2_EXIT='+$good.ExitCode)
if($good.ExitCode -ne 0){throw 'v3.0.16 updater did not apply real v3.0.17 RC1'}
CheckVersion (Join-Path $target 'VHWuWa.exe') '3.0.17'
CheckVersion (Join-Path $target 'VHWuWa.Updater.exe') '3.0.17'
if([IO.File]::ReadAllText((Join-Path $target 'Config\user.json')) -ne 'PRESERVE_V316_USER'){throw 'Update lost user config'}
if([IO.File]::ReadAllText((Join-Path $target 'old-only.txt')) -ne 'PRESERVE_OLD_ONLY'){throw 'Update lost non-overlapping files'}
$font=Join-Path $target 'content\font\WahuFont_100_P.pak'
if(-not(Test-Path $font)){throw 'Update missing default font'}
$fontSha=(Get-FileHash $font -Algorithm SHA256).Hash.ToLowerInvariant()
if($fontSha -ne 'ffc75bd057e15e47f288f2c1861b1f63b6443916e0cf255a987a055bd7da3632'){throw 'New default font hash mismatch'}
if(-not(Test-Path (Join-Path $target 'tools\repak.exe'))){throw 'Update missing font packer'}
$summary=[ordered]@{
 check='3.0.16 updater exact old executable -> 3.0.17 full RC1 ZIP'
 sha_rejected_before_modification=$true
 actual_package_applied=$true
 app_version='3.0.17.0'
 updater_version='3.0.17.0'
 config_preserved=$true
 older_files_preserved=$true
 lexend650_sha_verified=$true
 github_update_discovery_tested=$false
 app_health_handshake_tested_with_this_full_package=$false
 fixture_health_and_rollback_tested_separately=$true
 zip_sha256=$sha
}
$summary|ConvertTo-Json|Set-Content (Join-Path $root 'dist\VietHoa-WuWa-v3.0.17-UPGRADE-AUDIT.json') -Encoding UTF8
Write-Output ('PASS_REAL_V316_UPDATER '+($summary|ConvertTo-Json -Compress))
