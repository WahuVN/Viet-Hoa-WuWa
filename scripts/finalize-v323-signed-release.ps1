#requires -Version 5.1
param(
 [Parameter(Mandatory=$true)][string]$CandidateRoot,
 [Parameter(Mandatory=$true)][string]$SignedOutputRoot,
 [Parameter(Mandatory=$true)][string]$CodeSigningThumbprint,
 [string]$TimestampUrl = 'http://timestamp.digicert.com'
)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$version='3.0.23'
$sourceStage=Join-Path ([IO.Path]::GetFullPath($CandidateRoot)) 'stage'
$out=[IO.Path]::GetFullPath($SignedOutputRoot)
$stage=Join-Path $out 'stage'
$zip=Join-Path $out "VietHoa-WuWa-v$version.zip"
if(!(Test-Path $sourceStage -PathType Container)){throw "Missing candidate stage: $sourceStage"}
if(Test-Path $out){throw "Signed output already exists: $out"}
$cert=@(Get-ChildItem Cert:\CurrentUser\My,Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
 Where-Object {$_.Thumbprint -ieq $CodeSigningThumbprint -and $_.HasPrivateKey -and
  ($_.EnhancedKeyUsageList.ObjectId.Value -contains '1.3.6.1.5.5.7.3.3') -and $_.NotAfter -gt (Get-Date)}) | Select-Object -First 1
if($cert.Count -ne 1){throw 'Valid Code Signing certificate with private key not found for requested thumbprint.'}
$signtool=Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Filter signtool.exe -Recurse -File -ErrorAction SilentlyContinue |
 Where-Object {$_.FullName.EndsWith('\\x64\\signtool.exe',[StringComparison]::OrdinalIgnoreCase)} |
 Sort-Object FullName -Descending | Select-Object -First 1
if($null -eq $signtool){throw 'x64 signtool.exe not found.'}
New-Item -ItemType Directory -Path $stage -Force|Out-Null
& robocopy $sourceStage $stage /E /R:0 /W:0 /NFL /NDL /NJH /NJS /NP|Out-Null
if($LASTEXITCODE -gt 7){throw 'Stage copy failed.'}
foreach($name in @('VHWuWa.exe','VHWuWa.Updater.exe')) {
 $file=Join-Path $stage $name
 & $signtool.FullName sign /sha1 $CodeSigningThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $file
 if($LASTEXITCODE -ne 0){throw "Signing failed: $name"}
 & $signtool.FullName verify /pa /all $file | Out-Null
 if($LASTEXITCODE -ne 0){throw "Signature verify failed: $name"}
 $sig=Get-AuthenticodeSignature $file
 if($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Thumbprint -ine $CodeSigningThumbprint){throw "Authenticode mismatch: $name"}
}
$count=@(Get-ChildItem $stage -Recurse -File).Count
& python (Join-Path $repo 'scripts\pack-portable-zip.py') $stage $zip --version $version --expected-files $count
if($LASTEXITCODE -ne 0){throw 'Signed ZIP packaging failed.'}
& python (Join-Path $repo 'scripts\verify-v322-package.py') --stage $stage --zip $zip --version $version
if($LASTEXITCODE -ne 0){throw 'Signed ZIP byte verification failed.'}
$sha=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $out 'SHA256SUMS.txt'),"$sha  VietHoa-WuWa-v$version.zip"+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
[pscustomobject]@{Version=$version;Zip=$zip;Sha256=$sha;Files=$count;Signer=$cert[0].Subject;Thumbprint=$cert[0].Thumbprint;Signed=$true}|ConvertTo-Json -Compress
