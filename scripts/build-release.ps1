#requires -Version 5.1
# Retired dangerous generic packager; route to pinned R29 full release pipeline.
param(
 [string]$Version = '',
 [switch]$NoFonts,
 [string]$OutputRoot = '',
 [switch]$VerifyOnly
)
$ErrorActionPreference='Stop'
if ($NoFonts) {throw 'The R29 release requires Lexend 660 and 73-font catalog; -NoFonts is forbidden'}
if (-not $Version) {[xml]$p=Get-Content (Join-Path (Split-Path -Parent $PSScriptRoot) 'Directory.Build.props');$Version=[string]$p.Project.PropertyGroup.Version}
& (Join-Path $PSScriptRoot 'build-v322-win3-release.ps1') -Version $Version -OutputRoot $OutputRoot -VerifyOnly:$VerifyOnly
if (-not $?) {throw 'Canonical release pipeline failed'}
