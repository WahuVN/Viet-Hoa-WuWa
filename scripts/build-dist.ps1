#requires -Version 5.1
# Legacy V12/old-loader packing is forbidden. Always build pinned R29 instead.
param(
 [string]$Version = '',
 [string]$AppDichVersion = '',
 [string]$OutputRoot = '',
 [switch]$VerifyOnly
)
$ErrorActionPreference='Stop'
if ($AppDichVersion -and $AppDichVersion -ne '3.8.11') {
 throw 'Source template pinned to R29 resource 3.8.11. Refuse obsolete loader/content.'
}
if (-not $Version) {[xml]$p=Get-Content (Join-Path (Split-Path -Parent $PSScriptRoot) 'Directory.Build.props');$Version=[string]$p.Project.PropertyGroup.Version}
& (Join-Path $PSScriptRoot 'build-v322-win3-release.ps1') -Version $Version -OutputRoot $OutputRoot -VerifyOnly:$VerifyOnly
if (-not $?) {throw 'Canonical release pipeline failed'}
