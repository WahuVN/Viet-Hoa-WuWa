# Safe self-test for trusted Authenticode release gate. No OS changes.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$SignedMicrosoftFixture,
  [Parameter(Mandatory=$true)][string]$UnsignedWahuFixture
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$test = Join-Path $env:TEMP ("WAHU_SIGN_TEST_"+[guid]::NewGuid().ToString("N"))
$folder = Join-Path $test "content\loader"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
try {
  $signed = Join-Path $folder "verorg.dll"
  Copy-Item -LiteralPath $SignedMicrosoftFixture -Destination $signed
  & (Join-Path $repo "scripts\Assert-ReleaseSigning.ps1") -PackageRoot $test -RequiredRelativePaths @("content\loader\verorg.dll") | Out-Null
  if (-not $?) { throw "Valid signed fixture rejected." }
  Write-Output "PASS: valid Microsoft Authenticode fixture accepted."
  $unsigned = Join-Path $folder "version.dll"
  Copy-Item -LiteralPath $UnsignedWahuFixture -Destination $unsigned
  $beforeSigned = (Get-FileHash -LiteralPath $signed -Algorithm SHA256).Hash
  $beforeUnsigned = (Get-FileHash -LiteralPath $unsigned -Algorithm SHA256).Hash
  $caught = $false
  try {
    & (Join-Path $repo "scripts\Assert-ReleaseSigning.ps1") -PackageRoot $test -RequiredRelativePaths @("content\loader\verorg.dll","content\loader\version.dll") | Out-Null
  } catch {
    $caught = $true
  }
  if (-not $caught) { throw "Unsigned DLL unexpectedly passed release gate." }
  Write-Output "PASS: unsigned WAHU loader blocked by release gate."
  $caught = $false
  try {
    & (Join-Path $repo "scripts\Sign-ReleaseBinaries.ps1") -PackageRoot $test -CertificateThumbprint ("0"*40) | Out-Null
  } catch {
    $caught = $true
  }
  if (-not $caught) { throw "Missing certificate allowed signing." }
  if ((Get-FileHash -LiteralPath $signed -Algorithm SHA256).Hash -ne $beforeSigned -or
      (Get-FileHash -LiteralPath $unsigned -Algorithm SHA256).Hash -ne $beforeUnsigned) {
    throw "Certificate preflight modified fixture bytes."
  }
  Write-Output "PASS: missing certificate causes no modifications."

  $unsignedZip = Join-Path $env:TEMP ("WAHU_SIGN_UNSIGNED_"+[guid]::NewGuid().ToString("N")+".zip")
  $signedZip = Join-Path $env:TEMP ("WAHU_SIGN_VALID_"+[guid]::NewGuid().ToString("N")+".zip")
  try {
    Compress-Archive -Path (Join-Path $test "*") -DestinationPath $unsignedZip -Force
    $caught = $false
    try {
      & (Join-Path $repo "scripts\Assert-ReleaseZipSigning.ps1") -ZipPath $unsignedZip -RequiredRelativePaths @("content\loader\verorg.dll","content\loader\version.dll") | Out-Null
    } catch { $caught = $true }
    if (-not $caught) { throw "Unsigned ZIP unexpectedly passed signature gate." }
    Write-Output "PASS: final ZIP with unsigned loader blocked."

    Remove-Item -LiteralPath $unsigned -Force
    Compress-Archive -Path (Join-Path $test "*") -DestinationPath $signedZip -Force
    & (Join-Path $repo "scripts\Assert-ReleaseZipSigning.ps1") -ZipPath $signedZip -RequiredRelativePaths @("content\loader\verorg.dll") | Out-Null
    if (-not $?) { throw "Signed ZIP fixture unexpectedly rejected." }
    Write-Output "PASS: final ZIP with trusted Microsoft signature accepted."
  } finally {
    foreach ($z in @($unsignedZip,$signedZip)) {
      if (Test-Path -LiteralPath $z) { Remove-Item -LiteralPath $z -Force -ErrorAction SilentlyContinue }
    }
  }
  Write-Output "SIGNING_GATE_SELF_TEST_5_OF_5_PASS"
} finally {
  if (Test-Path -LiteralPath $test) {
    Remove-Item -LiteralPath $test -Recurse -Force -ErrorAction SilentlyContinue
  }
}
