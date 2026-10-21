# Read-only Smart App Control diagnostics (never disables any Windows protection).
# Example: powershell -File scripts/Inspect-SmartAppControlBlocks.ps1 -Hours 48
[CmdletBinding()]
param(
  [ValidateRange(1,168)][int]$Hours = 48,
  [string]$ReportPath = ""
)
$ErrorActionPreference = "Stop"
$since = (Get-Date).AddHours(-$Hours)
$log = "Microsoft-Windows-CodeIntegrity/Operational"
$ids = @(3076,3077,3089,3033,3034,3064,3065)
$events = @()
try {
  $events = @(Get-WinEvent -FilterHashtable @{
    LogName = $log
    Id = $ids
    StartTime = $since
  } -MaxEvents 400 -ErrorAction Stop)
} catch {
  if ($_.FullyQualifiedErrorId -match "NoMatchingEventsFound" -or
      $_.Exception.Message -match "No events were found") {
    $events = @()
  } else {
    throw ("Cannot read CodeIntegrity log. Try opening Event Viewer manually. "+$_.Exception.Message)
  }
}
$matched = @()
foreach ($evt in $events) {
  # Show only events that mention this launcher/game, not other app activity.
  $xml = [xml]$evt.ToXml()
  $details = @{}
  foreach ($entry in @($xml.Event.EventData.Data)) {
    if ($null -ne $entry.Name) { $details[[string]$entry.Name] = [string]$entry.'#text' }
  }
  $target = ($details.Values -join " ") + " " + [string]$evt.Message
  if ($target -notmatch '(?i)VHWuWa|WuWaVH|Wuthering Waves|Client-Win64-Shipping|WahuPakEditor|font-weight-instance|repak\.exe') {
    continue
  }
  $matched += [pscustomobject]@{
    time = $evt.TimeCreated.ToString("o")
    event_id = $evt.Id
    kind = if ($evt.Id -in 3077,3033,3065) { "BLOCKED" }
           elseif ($evt.Id -in 3076,3034,3064) { "AUDIT" }
           else { "SIGNATURE_INFO" }
    file = if ($details.ContainsKey("FileName")) { $details["FileName"] }
           elseif ($details.ContainsKey("FilePath")) { $details["FilePath"] }
           else { "" }
    process = if ($details.ContainsKey("ProcessName")) { $details["ProcessName"] } else { "" }
    correlation = [string]$xml.Event.System.Correlation.ActivityID
  }
}
$data = [ordered]@{
  source = $log
  from = $since.ToString("o")
  events_found = $matched.Count
  blocked = @($matched | Where-Object { $_.kind -eq "BLOCKED" }).Count
  note = "Event 3077 is enforcement block; 3076 is audit only. No setting has been changed."
  events = $matched
}
if ($ReportPath) {
  $output = [IO.Path]::GetFullPath($ReportPath)
  New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null
  [IO.File]::WriteAllText($output,($data|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
}
Write-Output ("SAC_WAHU_EVENTS="+$data.events_found+" BLOCKED="+$data.blocked)
foreach ($e in $matched) {
  Write-Output ("$($e.time) [$($e.event_id)] $($e.kind) FILE=$($e.file) PROCESS=$($e.process)")
}
if ($matched.Count -eq 0) {
  Write-Output "No matching CodeIntegrity events were found. This does not prove compatibility."
}
