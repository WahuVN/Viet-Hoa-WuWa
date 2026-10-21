# Validate the EXACT public ZIP, not only its source staging directory.
# Extracts only EXE/DLL to a temporary folder; never executes them.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$ZipPath,
  [string]$ReportPath = "",
  [string[]]$RequiredRelativePaths = @(
    "VHWuWa.exe","VHWuWa.Updater.exe",
    "content\loader\version.dll","content\loader\verorg.dll",
    "content\loader\WuWaVH.dll","editor\WahuPakEditor.exe",
    "tools\font-weight-instance.exe","tools\repak.exe"
  )
)
$ErrorActionPreference="Stop"
$zip=[IO.Path]::GetFullPath($ZipPath)
if(-not (Test-Path -LiteralPath $zip -PathType Leaf)){throw "Release ZIP not found: $zip"}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$temp=Join-Path $env:TEMP ("WAHU_FINAL_ZIP_SIGNATURE_"+[guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
$archive=$null
$files=0
$seen=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
try {
  $archive=[IO.Compression.ZipFile]::OpenRead($zip)
  foreach($entry in $archive.Entries){
    $name=$entry.FullName.Replace('/','\')
    if([string]::IsNullOrWhiteSpace($name) -or $name.EndsWith('\')){continue}
    $extension=[IO.Path]::GetExtension($name)
    if($extension -notin @('.exe','.dll')){continue}
    if([IO.Path]::IsPathRooted($name) -or $name.Contains(':') -or
       @($name.Split('\')|Where-Object{$_ -eq '..'}).Count -gt 0){
      throw "Unsafe executable ZIP entry: $name"
    }
    $dst=[IO.Path]::GetFullPath((Join-Path $temp $name))
    if(-not $dst.StartsWith($temp.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){
      throw "Executable ZIP entry outside test folder: $name"
    }
    if(-not $seen.Add($dst)){throw "Duplicate executable ZIP entry: $name"}
    New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null
    $srcStream=$entry.Open()
    try {
      $outStream=[IO.File]::Open($dst,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
      try { $srcStream.CopyTo($outStream) } finally { $outStream.Dispose() }
    } finally { $srcStream.Dispose() }
    $files++
  }
  if($files -eq 0){throw 'No EXE/DLL found in ZIP'}
  & (Join-Path $PSScriptRoot 'Assert-ReleaseSigning.ps1') -PackageRoot $temp -RequiredRelativePaths $RequiredRelativePaths -ReportPath $ReportPath
  if(-not $?){throw 'Final release ZIP signature audit failed'}
  Write-Output ("RELEASE_ZIP_SIGNATURE_PASS: $files signed executables; ZIP_SHA256="+
    (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant())
} finally {
  if($archive){$archive.Dispose()}
  if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue}
}
