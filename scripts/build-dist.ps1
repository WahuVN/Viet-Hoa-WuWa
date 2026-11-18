#requires -Version 5.1
param(
 [string]$Version = '',
 [string]$AppDichVersion = '',
 [string]$OutputRoot = '',
 [switch]$VerifyOnly,
 [switch]$RequireAuthenticode
)
$ErrorActionPreference='Stop'
if ($AppDichVersion -and $AppDichVersion -ne '3.8.11') {throw 'Source template pinned to R29 resource 3.8.11. Refuse obsolete loader/content.'}
if (-not $Version) {[xml]$p=Get-Content (Join-Path (Split-Path -Parent $PSScriptRoot) 'Directory.Build.props');$Version=[string]$p.Project.PropertyGroup.Version}
& (Join-Path $PSScriptRoot 'build-standard-pc-release.ps1') -Version $Version -OutputRoot $OutputRoot -VerifyOnly:$VerifyOnly -RequireAuthenticode:$RequireAuthenticode
if (-not $?) {throw 'Canonical release pipeline failed'}
