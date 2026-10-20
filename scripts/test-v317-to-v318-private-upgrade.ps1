# Isolated actual v3.0.17 updater -> prepared local v3.0.18 full ZIP.
# No GitHub, Discord, game directory, or current end-user installation is touched.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$old=Join-Path $root 'build\v317-auto-clean-prep\stage'
$release=Join-Path $root 'dist\release-prep\v3.0.18'
$zip=Join-Path $release 'VietHoa-WuWa-v3.0.18.zip'
$preflight=Join-Path $release 'RELEASE_PREFLIGHT.json'
$e2e=Join-Path $release 'UPGRADE_317_TO_318_QA.json'
$work=Join-Path $root ('build\v318-prep-20261020\updater-e2e-'+[guid]::NewGuid().ToString('N'))
$target=Join-Path $work 'isolated-installed-v317'
$runner=Join-Path $work 'VHWuWa.Updater.run.exe'

foreach($item in @($zip,$preflight,(Join-Path $old 'VHWuWa.exe'),(Join-Path $old 'VHWuWa.Updater.exe'))){
 if(!(Test-Path -LiteralPath $item)){throw ('Missing fixture '+$item)}
}
if(Test-Path $e2e){throw 'Refusing to overwrite an existing upgrade QA report'}
if((Get-PSDrive D).Free -lt 2GB){throw 'Upgrade fixture needs 2 GB available'}
New-Item -ItemType Directory -Force $target,(Join-Path $target 'Config')|Out-Null
Copy-Item (Join-Path $old 'VHWuWa.exe') (Join-Path $target 'VHWuWa.exe')
Copy-Item (Join-Path $old 'VHWuWa.Updater.exe') (Join-Path $target 'VHWuWa.Updater.exe')
Copy-Item (Join-Path $old 'VHWuWa.Updater.exe') $runner
[IO.File]::WriteAllText((Join-Path $target 'Config\user.json'),'DO_NOT_OVERWRITE_USER_CONFIG_317_TO_318')
[IO.File]::WriteAllText((Join-Path $target 'user-added-note.txt'),'RETAIN_UNRELATED_FILE')

function CheckVersion([string]$path,[string]$version){
 $actual=[Diagnostics.FileVersionInfo]::GetVersionInfo($path).FileVersion
 if(-not $actual.StartsWith("$version.")){throw ('EXE version mismatch '+$actual+' expected '+$version)}
}
CheckVersion (Join-Path $target 'VHWuWa.exe') '3.0.17'
CheckVersion (Join-Path $target 'VHWuWa.Updater.exe') '3.0.17'
$originalSha=(Get-FileHash (Join-Path $target 'VHWuWa.exe') -Algorithm SHA256).Hash
$zipSha=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output '[QA] Incorrect SHA must be rejected before writing'
$badArgs=@('--zip',$zip,'--target',$target,'--expected-version','3.0.18','--sha256',('0'*64),'--no-relaunch')
$bad=Start-Process -FilePath $runner -ArgumentList $badArgs -Wait -PassThru -WindowStyle Hidden
if($bad.ExitCode -ne 4){throw ('Incorrect SHA not rejected; exit='+$bad.ExitCode)}
if((Get-FileHash (Join-Path $target 'VHWuWa.exe') -Algorithm SHA256).Hash -ne $originalSha){
 throw 'Bad SHA unexpectedly changed installed application'
}
Write-Output '[QA] Apply real complete ZIP, no relaunch'
$goodArgs=@('--zip',$zip,'--target',$target,'--expected-version','3.0.18','--sha256',$zipSha,'--no-relaunch')
$good=Start-Process -FilePath $runner -ArgumentList $goodArgs -Wait -PassThru -WindowStyle Hidden
if($good.ExitCode -ne 0){throw ('Updater v3.0.17 failed real 3.0.18 ZIP; exit='+$good.ExitCode)}
CheckVersion (Join-Path $target 'VHWuWa.exe') '3.0.18'
CheckVersion (Join-Path $target 'VHWuWa.Updater.exe') '3.0.18'
if([IO.File]::ReadAllText((Join-Path $target 'Config\user.json')) -ne 'DO_NOT_OVERWRITE_USER_CONFIG_317_TO_318'){
 throw 'Existing user config was overwritten'
}
if([IO.File]::ReadAllText((Join-Path $target 'user-added-note.txt')) -ne 'RETAIN_UNRELATED_FILE'){
 throw 'Existing user-owned file was removed'
}
$manifest=Get-Content $preflight -Raw -Encoding UTF8|ConvertFrom-Json
if((Get-FileHash (Join-Path $target 'content\font\WahuFont_100_P.pak') -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.font_pak_sha256){
 throw 'Updater did not deliver new B119+Lexend font pack'
}
if((Get-FileHash (Join-Path $target 'content\WuWaVH_EN_99_P.pak') -Algorithm SHA256).Hash -ne
   '44350C79CBF9CB307752682F0E0F3A31AF983CDC1FEA4B24403B7395237DF547'){
 throw 'Updater altered English translation PAK'
}
$summary=[ordered]@{
  test='local v3.0.17 updater -> private v3.0.18 full ZIP'
  status='PASS'
  wrong_sha_rejected_before_modification=$true
  real_zip_applied=$true
  app_version='3.0.18.0'
  updater_version='3.0.18.0'
  user_config_preserved=$true
  unrelated_user_file_preserved=$true
  font_10_assets_pack_sha256_confirmed=$true
  game_runtime_tested=$false
  old_updater_exit_invalid_sha=$bad.ExitCode
  old_updater_exit_real_upgrade=$good.ExitCode
  zip_sha256=$zipSha
  isolated_directory=$work
  github_published=$false
}
($summary|ConvertTo-Json -Depth 4)|Set-Content $e2e -Encoding UTF8
Write-Output ('PASS_317_TO_318_PRIVATE '+($summary|ConvertTo-Json -Compress))
